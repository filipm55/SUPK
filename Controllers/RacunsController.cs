using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.Services;
using SUPK.ViewModels;
using System.Text.Json;

namespace SUPK.Controllers
{
    public class RacunsController : Controller
    {
        private readonly CaffeBarDbContext _context;
        private readonly IRacunService _racunService;

        public RacunsController( CaffeBarDbContext context, IRacunService racunService)
        {
            _context = context;
            _racunService = racunService;
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

            await _racunService.CreateAsync(vm);

            return RedirectToAction("Index", "Home");
           
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

            ModelState.Remove("Racun.Konobar");
            ModelState.Remove("Racun.Stol");
            ModelState.Remove("Racun.Narudzbas");

            if (vm.Racun.VrijemeZatvaranja.HasValue &&
                vm.Racun.VrijemeZatvaranja.Value < vm.Racun.VrijemeOtvaranja)
            {
                ModelState.AddModelError(
                    "Racun.VrijemeZatvaranja",
                    "Vrijeme zatvaranja ne smije biti prije vremena otvaranja.");
            }

            if (vm.Racun.VrijemeZatvaranja.HasValue &&
                !vm.Racun.NacinPlacanja.HasValue)
            {
                ModelState.AddModelError(
                    "Racun.NacinPlacanja",
                    "Račun mora sadržavati način plaćanja.");
            }

            if (vm.Racun.NacinPlacanja.HasValue &&
                !vm.Racun.VrijemeZatvaranja.HasValue)
            {
                ModelState.AddModelError(
                    "Racun.VrijemeZatvaranja",
                    "Račun mora sadržavati vrijeme zatvaranja.");
            }

            vm.Narudzbe ??= new();

            // Provjera duplikata proizvoda
            for (var i = 0; i < vm.Narudzbe.Count; i++)
            {
                var dup = (vm.Narudzbe[i].Stavke ?? new List<StavkaEditViewModel>())
                    .Where(s => s.ProizvodId.HasValue &&
                                s.Kolicina.HasValue &&
                                s.Kolicina.Value > 0)
                    .GroupBy(s => s.ProizvodId!.Value)
                    .Any(g => g.Count() > 1);

                if (dup)
                {
                    ModelState.AddModelError(
                        "",
                        $"U narudžbi #{i + 1} nije dozvoljeno dodati isti proizvod više puta.");
                }
            }

            //Provjera da stol nema drugi otvoreni račun
            if (vm.Racun.StolId.HasValue)
            {
                var stolImaOtvorenRacun = await _context.Racuns.AnyAsync(r =>
                    r.StolId == vm.Racun.StolId &&
                    !r.VrijemeZatvaranja.HasValue &&
                    r.RacunId != vm.Racun.RacunId);

                if (stolImaOtvorenRacun)
                {
                    ModelState.AddModelError(
                        "Racun.StolId",
                        "Odabrani stol već ima otvoren račun.");
                }
            }
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(vm);
            }

            try
            {
                await _racunService.UpdateAsync(vm);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RacunExists(vm.Racun.RacunId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction("Index", "Home");
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
