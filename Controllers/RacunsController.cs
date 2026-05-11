using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    if(stavkaVm.ProizvodId.HasValue && stavkaVm.Kolicina.HasValue) 
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

            var racun = await _context.Racuns.FindAsync(id);
            if (racun == null)
            {
                return NotFound();
            }
            ViewData["KonobarId"] = new SelectList(_context.Konobars, "KonobarId", "KonobarId", racun.KonobarId);
            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "StolId", racun.StolId);
            return View(racun);
        }

        // POST: Racuns/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RacunId,VrijemeOtvaranja,VrijemeZatvaranja,NacinPlacanja, UkupnaCijena,StolId,KonobarId")] Racun racun)
        {
            if (id != racun.RacunId)
            {
                return NotFound();
            }

            // Očistite navigacijska svojstva iz ModelState validacije
            ModelState.Remove("Konobar");
            ModelState.Remove("Stol");
            ModelState.Remove("Narudzbas");

            //if (racun.VrijemeZatvaranja.HasValue && !racun.NacinPlacanja.HasValue)
            //{
            //    ModelState.AddModelError("NacinPlacanja", "Račun mora sadržavati način plaćanja.");
            //}

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(racun);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RacunExists(racun.RacunId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["KonobarId"] = new SelectList(_context.Konobars, "KonobarId", "KonobarId", racun.KonobarId);
            ViewData["StolId"] = new SelectList(_context.Stols, "StolId", "StolId", racun.StolId);
            return View(racun);
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
