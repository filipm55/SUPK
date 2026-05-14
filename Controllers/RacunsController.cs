using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.ViewModels;
using System.Text.Json;

namespace SUPK.Controllers
{
    public class RacunsController : Controller
    {
        private readonly CaffeBarDbContext _context;

        public RacunsController(CaffeBarDbContext context)
        {
            _context = context;
        }

        // GET: Racuns
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // GET: Racuns/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racuns
                .Include(r => r.Konobar)
                .Include(r => r.Stol)
                .Include(r => r.Narudzbas)
                    .ThenInclude(n => n.Stavkanarudzbes)
                        .ThenInclude(s => s.Proizvod)
                .FirstOrDefaultAsync(m => m.RacunId == id);

            if (racun == null)
            {
                return NotFound();
            }

            return View(racun);
        }

        // GET: Racuns/Create
        public async Task<IActionResult> Create()
        {
            var konobari = await _context.Konobars
                .Select(k => new { k.KonobarId, PunoIme = k.Ime + " " + k.Prezime })
                .ToListAsync();

            ViewData["KonobarId"] = new SelectList(konobari, "KonobarId", "PunoIme");

            ViewData["StolId"] = new SelectList(
                await _context.Stols.ToListAsync(),
                "StolId",
                "BrojStola");

            var proizvodiList = await _context.Proizvods
                .Select(p => new { p.ProizvodId, p.Naziv, Cijena = p.Cijena })
                .ToListAsync();

            ViewData["Proizvodi"] = new SelectList(proizvodiList, "ProizvodId", "Naziv");
            ViewBag.ProizvodiJson = JsonSerializer.Serialize(proizvodiList);

            var vm = new RacunCreateViewModel();

            var now = DateTime.Now;
            vm.Racun.VrijemeOtvaranja = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            vm.Stavke.Add(new StavkaViewModel());

            return View(vm);
        }

        // POST: Racuns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RacunCreateViewModel vm)
        {
            ModelState.Remove("Racun.Konobar");
            ModelState.Remove("Racun.Stol");
            ModelState.Remove("Racun.Narudzbas");

            if (vm.Racun.VrijemeZatvaranja.HasValue && vm.Racun.VrijemeZatvaranja.Value < vm.Racun.VrijemeOtvaranja)
            {
                ModelState.AddModelError("Racun.VrijemeZatvaranja", "Vrijeme zatvaranja ne smije biti prije vremena otvaranja.");
            }

            if (vm.Racun.NacinPlacanja.HasValue && !vm.Racun.VrijemeZatvaranja.HasValue)
            {
                ModelState.AddModelError("Racun.VrijemeZatvaranja", "Ako je odabran način plaćanja, morate upisati i vrijeme zatvaranja.");
            }

            if (vm.Racun.VrijemeZatvaranja.HasValue && !vm.Racun.NacinPlacanja.HasValue)
            {
                ModelState.AddModelError("Racun.NacinPlacanja", "Ako je upisano vrijeme zatvaranja, morate odabrati način plaćanja.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(vm);
            }


            if (vm.Stavke == null || !vm.Stavke.Any())
            {
                ModelState.AddModelError("", "Račun mora imati barem jednu stavku.");
                await LoadDropdowns();
                return View(vm);
            }

            vm.Stavke = vm.Stavke
                .Where(s => s.ProizvodId > 0 && s.Kolicina > 0)
                .ToList();

            if (!vm.Stavke.Any())
            {
                ModelState.AddModelError("", "Račun mora imati barem jednu valjanu stavku.");
                await LoadDropdowns();
                return View(vm);
            }

            if (vm.Stavke
                .Where(s => s.ProizvodId.HasValue)
                .GroupBy(s => s.ProizvodId!.Value)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("", "Nije dozvoljeno dodati isti proizvod više puta u stavkama. Povećajte količinu umjesto duplikata.");
                await LoadDropdowns();
                return View(vm);
            }

            if (vm.Racun.StolId.HasValue)
            {
                var stolImaOtvorenRacun = await _context.Racuns
                    .AnyAsync(r => r.StolId == vm.Racun.StolId && !r.VrijemeZatvaranja.HasValue);

                if (stolImaOtvorenRacun)
                {
                    ModelState.AddModelError("Racun.StolId", "Odabrani stol već ima otvoren račun.");
                    await LoadDropdowns();
                    return View(vm);
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Spremi racun
                _context.Racuns.Add(vm.Racun);
                await _context.SaveChangesAsync();

                // Kreiraj narudzbu
                var narudzba = new Narudzba
                {
                    VrijemeNarudzbe = DateTime.Now,
                    RacunId = vm.Racun.RacunId
                };

                _context.Narudzbas.Add(narudzba);
                await _context.SaveChangesAsync();

                //Spremi stavke
                foreach (var stavkaVm in vm.Stavke)
                {
                    if (stavkaVm.ProizvodId.HasValue && stavkaVm.Kolicina.HasValue)
                    {
                        var stavka = new Stavkanarudzbe
                        {
                            NarudzbaId = narudzba.NarudzbaId,
                            ProizvodId = stavkaVm.ProizvodId.Value,
                            Kolicina = stavkaVm.Kolicina.Value
                        };

                        _context.Stavkanarudzbes.Add(stavka);
                    }
                }

                await _context.SaveChangesAsync();

                // Izracunaj ukupnu cijenu racuna iz stavki
                var total = await _context.Stavkanarudzbes
                    .Where(s => s.NarudzbaId == narudzba.NarudzbaId)
                    .Include(s => s.Proizvod)
                    .Select(s => (decimal?)s.Kolicina * s.Proizvod.Cijena)
                    .SumAsync();

                vm.Racun.UkupnaCijena = total ?? 0m;
                _context.Racuns.Update(vm.Racun);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // GET: Racuns/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racuns
                .Include(r => r.Narudzbas)
                    .ThenInclude(n => n.Stavkanarudzbes)
                .FirstOrDefaultAsync(r => r.RacunId == id);
            if (racun == null)
            {
                return NotFound();
            }

            ViewData["KonobarId"] = new SelectList(
                _context.Konobars.Select(k => new
                {
                    k.KonobarId,
                    PunoIme = k.Ime + " " + k.Prezime
                }),
                "KonobarId",
                "PunoIme",
                racun.KonobarId);

            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "BrojStola", racun.StolId);

            ViewData["NacinPlacanja"] = new SelectList(
                Enum.GetValues(typeof(TipPlacanja))
                    .Cast<TipPlacanja>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                racun.NacinPlacanja);

            var proizvodiList = await _context.Proizvods
                .Select(p => new { p.ProizvodId, p.Naziv, Cijena = p.Cijena })
                .ToListAsync();
            ViewData["Proizvodi"] = new SelectList(proizvodiList, "ProizvodId", "Naziv");
            ViewBag.ProizvodiJson = JsonSerializer.Serialize(proizvodiList);

            var vm = new RacunEditViewModel
            {
                Racun = racun,
                Narudzbe = racun.Narudzbas.Select(n => new NarudzbaEditViewModel
                {
                    NarudzbaId = n.NarudzbaId,
                    VrijemeNarudzbe = n.VrijemeNarudzbe,
                    Stavke = n.Stavkanarudzbes.Select(s => new StavkaEditViewModel
                    {
                        StavkaId = s.StavkaId,
                        ProizvodId = s.ProizvodId,
                        Kolicina = s.Kolicina
                    }).ToList()
                }).ToList()
            };

            if (!vm.Narudzbe.Any())
            {
                vm.Narudzbe.Add(new NarudzbaEditViewModel { VrijemeNarudzbe = DateTime.Now });
            }

            foreach (var narudzbaVm in vm.Narudzbe)
            {
                if (!narudzbaVm.Stavke.Any())
                    narudzbaVm.Stavke.Add(new StavkaEditViewModel());
            }

            return View(vm);
        }

        // POST: Racuns/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RacunEditViewModel vm)
        {
            if (id != vm.Racun.RacunId)
            {
                return NotFound();
            }

            var racunFromDb = await _context.Racuns
                .Include(r => r.Narudzbas)
                    .ThenInclude(n => n.Stavkanarudzbes)
                .FirstOrDefaultAsync(r => r.RacunId == id);
            if (racunFromDb == null)
            {
                return NotFound();
            }

            ModelState.Remove("Racun.Konobar");
            ModelState.Remove("Racun.Stol");
            ModelState.Remove("Racun.Narudzbas");

            if (vm.Racun.VrijemeZatvaranja.HasValue && vm.Racun.VrijemeZatvaranja.Value < vm.Racun.VrijemeOtvaranja)
            {
                ModelState.AddModelError("Racun.VrijemeZatvaranja", "Vrijeme zatvaranja ne smije biti prije vremena otvaranja.");
            }

            if (vm.Racun.VrijemeZatvaranja.HasValue && !vm.Racun.NacinPlacanja.HasValue)
            {
                ModelState.AddModelError("Racun.NacinPlacanja", "Račun mora sadržavati način plaćanja.");
            }

            if (vm.Racun.NacinPlacanja.HasValue && !vm.Racun.VrijemeZatvaranja.HasValue)
            {
                ModelState.AddModelError("Racun.VrijemeZatvaranja", "Račun mora sadržavati vrijeme zatvaranja.");
            }

            vm.Narudzbe ??= new();

            // ne dopusti duple proizvode unutar iste narudžbe
            for (var i = 0; i < vm.Narudzbe.Count; i++)
            {
                var dup = (vm.Narudzbe[i].Stavke ?? new List<StavkaEditViewModel>())
                    .Where(s => s.ProizvodId.HasValue && s.Kolicina.HasValue && s.Kolicina.Value > 0)
                    .GroupBy(s => s.ProizvodId!.Value)
                    .Any(g => g.Count() > 1);

                if (dup)
                {
                    ModelState.AddModelError("", $"U narudžbi #{i + 1} nije dozvoljeno dodati isti proizvod više puta. Povećajte količinu umjesto duplikata.");
                }
            }

            // stol ne smije imati drugi otvoreni račun
            if (vm.Racun.StolId.HasValue)
            {
                var stolImaOtvorenRacun = await _context.Racuns.AnyAsync(r =>
                    r.StolId == vm.Racun.StolId &&
                    !r.VrijemeZatvaranja.HasValue &&
                    r.RacunId != vm.Racun.RacunId);

                if (stolImaOtvorenRacun)
                {
                    ModelState.AddModelError("Racun.StolId", "Odabrani stol već ima otvoren račun.");
                }
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    racunFromDb.VrijemeOtvaranja = vm.Racun.VrijemeOtvaranja;
                    racunFromDb.VrijemeZatvaranja = vm.Racun.VrijemeZatvaranja;
                    racunFromDb.NacinPlacanja = vm.Racun.NacinPlacanja;
                    racunFromDb.StolId = vm.Racun.StolId;
                    racunFromDb.KonobarId = vm.Racun.KonobarId;

                    var postedNarudzbaIds = vm.Narudzbe.Where(n => n.NarudzbaId.HasValue).Select(n => n.NarudzbaId!.Value).ToHashSet();

                    var narudzbasToRemove = racunFromDb.Narudzbas.Where(n => !postedNarudzbaIds.Contains(n.NarudzbaId)).ToList();
                    _context.Narudzbas.RemoveRange(narudzbasToRemove);

                    foreach (var nVm in vm.Narudzbe)
                    {
                        Narudzba narudzbaEntity;
                        if (nVm.NarudzbaId.HasValue && nVm.NarudzbaId.Value > 0)
                        {
                            narudzbaEntity = racunFromDb.Narudzbas.FirstOrDefault(n => n.NarudzbaId == nVm.NarudzbaId.Value)!;
                            if (narudzbaEntity == null) continue;
                        }
                        else
                        {
                            narudzbaEntity = new Narudzba { VrijemeNarudzbe = DateTime.Now, RacunId = racunFromDb.RacunId };
                            _context.Narudzbas.Add(narudzbaEntity);
                        }

                        var validStavke = nVm.Stavke.Where(s => s.ProizvodId.HasValue && s.Kolicina.HasValue && s.Kolicina.Value > 0).ToList();
                        var postedStavkaIds = validStavke.Where(s => s.StavkaId.HasValue).Select(s => s.StavkaId!.Value).ToHashSet();

                        if (narudzbaEntity.NarudzbaId > 0)
                        {
                            var stavkasToRemove = narudzbaEntity.Stavkanarudzbes.Where(s => !postedStavkaIds.Contains(s.StavkaId)).ToList();
                            _context.Stavkanarudzbes.RemoveRange(stavkasToRemove);
                        }

                        foreach (var sVm in validStavke)
                        {
                            if (sVm.StavkaId.HasValue && sVm.StavkaId.Value > 0)
                            {
                                var existing = narudzbaEntity.Stavkanarudzbes.FirstOrDefault(s => s.StavkaId == sVm.StavkaId.Value);
                                if (existing != null)
                                {
                                    existing.ProizvodId = sVm.ProizvodId!.Value;
                                    existing.Kolicina = sVm.Kolicina!.Value;
                                }
                            }
                            else
                            {
                                _context.Stavkanarudzbes.Add(new Stavkanarudzbe
                                {
                                    Narudzba = narudzbaEntity,
                                    ProizvodId = sVm.ProizvodId!.Value,
                                    Kolicina = sVm.Kolicina!.Value
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    var racunTotal = await _context.Narudzbas
                        .Where(n => n.RacunId == racunFromDb.RacunId)
                        .SelectMany(n => n.Stavkanarudzbes)
                        .Include(s => s.Proizvod)
                        .Select(s => (decimal?)s.Kolicina * s.Proizvod.Cijena)
                        .SumAsync();

                    racunFromDb.UkupnaCijena = racunTotal ?? 0m;
                    _context.Racuns.Update(racunFromDb);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return RedirectToAction("Index", "Home");
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();

                    if (!RacunExists(vm.Racun.RacunId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var proizvodiListDb = await _context.Proizvods
                .Select(p => new { p.ProizvodId, p.Naziv, Cijena = p.Cijena })
                .ToListAsync();
            ViewData["Proizvodi"] = new SelectList(proizvodiListDb, "ProizvodId", "Naziv");
            ViewBag.ProizvodiJson = JsonSerializer.Serialize(proizvodiListDb);

            ViewData["KonobarId"] = new SelectList(
                _context.Konobars.Select(k => new
                {
                    k.KonobarId,
                    PunoIme = k.Ime + " " + k.Prezime
                }),
                "KonobarId",
                "PunoIme",
                vm.Racun.KonobarId);

            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "BrojStola", vm.Racun.StolId);

            ViewData["NacinPlacanja"] = new SelectList(
                Enum.GetValues(typeof(TipPlacanja))
                    .Cast<TipPlacanja>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                vm.Racun.NacinPlacanja);
            return View(vm);
        }

        // GET: Racuns/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racuns
                .Include(r => r.Konobar)
                .Include(r => r.Stol)
                .FirstOrDefaultAsync(m => m.RacunId == id);
            if (racun == null)
            {
                return NotFound();
            }

            return View(racun);
        }

        // POST: Racuns/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var racun = await _context.Racuns.FindAsync(id);
            if (racun != null)
            {
                _context.Racuns.Remove(racun);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), "Home");
        }

        private bool RacunExists(int id)
        {
            return _context.Racuns.Any(e => e.RacunId == id);
        }

        private async Task LoadDropdowns()
        {
            ViewData["KonobarId"] = new SelectList(
                await _context.Konobars
                    .Select(k => new
                    {
                        k.KonobarId,
                        PunoIme = k.Ime + " " + k.Prezime
                    })
                    .ToListAsync(),
                "KonobarId",
                "PunoIme");

            ViewData["StolId"] = new SelectList(
                await _context.Stols.ToListAsync(),
                "StolId",
                "BrojStola");

            ViewData["Proizvodi"] = new SelectList(
                await _context.Proizvods.ToListAsync(),
                "ProizvodId",
                "Naziv");
        }

    }
}
