using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.ViewModels;

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
        public async Task<IActionResult> Index()
        {
            var caffeBarDbContext = _context.Racuns.Include(r => r.Konobar).Include(r => r.Stol);
            return View(await caffeBarDbContext.ToListAsync());
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
        public IActionResult Create()
        {
            ViewData["KonobarId"] = new SelectList(
                _context.Konobars.Select(k => new
                {
                    k.KonobarId,
                    PunoIme = k.Ime + " " + k.Prezime
                }),
                "KonobarId",
                "PunoIme");

            ViewData["StolId"] = new SelectList(
                _context.Stols,
                "StolId",
                "BrojStola");

            ViewData["Proizvodi"] = new SelectList(
                _context.Proizvods,
                "ProizvodId",
                "Naziv");

            ViewData["NacinPlacanja"] = new SelectList(
                Enum.GetValues(typeof(TipPlacanja))
                    .Cast<TipPlacanja>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text");

            var vm = new RacunCreateViewModel();

            var now = DateTime.Now;
            vm.Racun.VrijemeOtvaranja = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            vm.Stavke.Add(new StavkaViewModel());

            return View(vm);
        }

        // POST: Racuns/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RacunCreateViewModel vm)
        {
            ModelState.Remove("Racun.Konobar");
            ModelState.Remove("Racun.Stol");
            ModelState.Remove("Racun.Narudzbas");

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

                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
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
            ViewData["KonobarId"] = new SelectList(_context.Konobars, "KonobarId", "KonobarId", racun.KonobarId);
            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "StolId", racun.StolId);
            ViewData["Proizvodi"] = new SelectList(
                await _context.Proizvods.ToListAsync(),
                "ProizvodId",
                "Naziv");

            ViewData["NacinPlacanja"] = new SelectList(
                Enum.GetValues(typeof(TipPlacanja))
                    .Cast<TipPlacanja>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                racun.NacinPlacanja);

            var vm = new RacunEditViewModel
            {
                Racun = racun,
                Stavke = racun.Narudzbas
                    .SelectMany(n => n.Stavkanarudzbes)
                    .Select(s => new StavkaEditViewModel
                    {
                        StavkaId = s.StavkaId,
                        ProizvodId = s.ProizvodId,
                        Kolicina = s.Kolicina
                    })
                    .ToList()
            };

            if (!vm.Stavke.Any())
            {
                vm.Stavke.Add(new StavkaEditViewModel());
            }

            return View(vm);
        }

        // POST: Racuns/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

            // Očistite navigacijska svojstva iz ModelState validacije
            ModelState.Remove("Racun.Konobar");
            ModelState.Remove("Racun.Stol");
            ModelState.Remove("Racun.Narudzbas");

            if (vm.Racun.VrijemeZatvaranja.HasValue && !vm.Racun.NacinPlacanja.HasValue)
            {
                ModelState.AddModelError("Racun.NacinPlacanja", "Račun mora sadržavati način plaćanja.");
            }

            if (vm.Racun.NacinPlacanja.HasValue && !vm.Racun.VrijemeZatvaranja.HasValue)
            {
                ModelState.AddModelError("Racun.VrijemeZatvaranja", "Račun mora sadržavati vrijeme zatvaranja.");
            }

            vm.Stavke ??= new();

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

                    var narudzba = racunFromDb.Narudzbas.OrderByDescending(n => n.NarudzbaId).FirstOrDefault();
                    if (narudzba == null)
                    {
                        narudzba = new Narudzba
                        {
                            VrijemeNarudzbe = DateTime.Now,
                            RacunId = racunFromDb.RacunId
                        };
                        _context.Narudzbas.Add(narudzba);
                        await _context.SaveChangesAsync();
                    }

                    var validStavke = vm.Stavke
                        .Where(s => s.ProizvodId.HasValue && s.Kolicina.HasValue && s.Kolicina.Value > 0)
                        .ToList();

                    if (!validStavke.Any())
                    {
                        ModelState.AddModelError("", "Račun mora imati barem jednu valjanu stavku.");
                        throw new InvalidOperationException("No valid items");
                    }

                    var postedIds = validStavke
                        .Where(s => s.StavkaId.HasValue)
                        .Select(s => s.StavkaId!.Value)
                        .ToHashSet();

                    var toRemove = narudzba.Stavkanarudzbes
                        .Where(s => !postedIds.Contains(s.StavkaId))
                        .ToList();

                    if (toRemove.Count > 0)
                    {
                        _context.Stavkanarudzbes.RemoveRange(toRemove);
                    }

                    foreach (var stavkaVm in validStavke)
                    {
                        if (stavkaVm.StavkaId.HasValue)
                        {
                            var existing = narudzba.Stavkanarudzbes.FirstOrDefault(s => s.StavkaId == stavkaVm.StavkaId.Value);
                            if (existing != null)
                            {
                                existing.ProizvodId = stavkaVm.ProizvodId!.Value;
                                existing.Kolicina = stavkaVm.Kolicina!.Value;
                                continue;
                            }
                        }

                        _context.Stavkanarudzbes.Add(new Stavkanarudzbe
                        {
                            NarudzbaId = narudzba.NarudzbaId,
                            ProizvodId = stavkaVm.ProizvodId!.Value,
                            Kolicina = stavkaVm.Kolicina!.Value
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();

                    if (!RacunExists(vm.Racun.RacunId))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch
                {
                    await transaction.RollbackAsync();
                }
            }
            ViewData["KonobarId"] = new SelectList(_context.Konobars, "KonobarId", "KonobarId", vm.Racun.KonobarId);
            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "StolId", vm.Racun.StolId);
            ViewData["Proizvodi"] = new SelectList(
                await _context.Proizvods.ToListAsync(),
                "ProizvodId",
                "Naziv");

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
            return RedirectToAction(nameof(Index));
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
