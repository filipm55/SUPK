using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;

namespace SUPK.Controllers
{
    public class StavkanarudzbesController : Controller
    {
        private readonly CaffeBarDbContext _context;

        public StavkanarudzbesController(CaffeBarDbContext context)
        {
            _context = context;
        }

        // GET: Stavkanarudzbes
        public async Task<IActionResult> Index()
        {
            var caffeBarDbContext = _context.Stavkanarudzbes.Include(s => s.Narudzba).Include(s => s.Proizvod);
            return View(await caffeBarDbContext.ToListAsync());
        }

        // GET: Stavkanarudzbes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stavkanarudzbe = await _context.Stavkanarudzbes
                .Include(s => s.Narudzba)
                .Include(s => s.Proizvod)
                .FirstOrDefaultAsync(m => m.StavkaId == id);
            if (stavkanarudzbe == null)
            {
                return NotFound();
            }

            return View(stavkanarudzbe);
        }

        // GET: Stavkanarudzbes/Create
        public IActionResult Create()
        {
            ViewData["NarudzbaId"] = new SelectList(_context.Narudzbas, "NarudzbaId", "NarudzbaId");
            ViewData["ProizvodId"] = new SelectList(_context.Proizvods, "ProizvodId", "Naziv");
            return View();
        }

        // POST: Stavkanarudzbes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StavkaId,Kolicina,NarudzbaId,ProizvodId")] Stavkanarudzbe stavkanarudzbe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stavkanarudzbe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["NarudzbaId"] = new SelectList(_context.Narudzbas, "NarudzbaId", "NarudzbaId", stavkanarudzbe.NarudzbaId);
            ViewData["ProizvodId"] = new SelectList(_context.Proizvods, "ProizvodId", "ProizvodId", stavkanarudzbe.ProizvodId);
            return View(stavkanarudzbe);
        }

        // GET: Stavkanarudzbes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stavkanarudzbe = await _context.Stavkanarudzbes.FindAsync(id);
            if (stavkanarudzbe == null)
            {
                return NotFound();
            }
            ViewData["NarudzbaId"] = new SelectList(_context.Narudzbas, "NarudzbaId", "NarudzbaId", stavkanarudzbe.NarudzbaId);
            ViewData["ProizvodId"] = new SelectList(_context.Proizvods, "ProizvodId", "Naziv", stavkanarudzbe.ProizvodId);
            return View(stavkanarudzbe);
        }

        // POST: Stavkanarudzbes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StavkaId,Kolicina,NarudzbaId,ProizvodId")] Stavkanarudzbe stavkanarudzbe)
        {
            if (id != stavkanarudzbe.StavkaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stavkanarudzbe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StavkanarudzbeExists(stavkanarudzbe.StavkaId))
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
            ViewData["NarudzbaId"] = new SelectList(_context.Narudzbas, "NarudzbaId", "NarudzbaId", stavkanarudzbe.NarudzbaId);
            ViewData["ProizvodId"] = new SelectList(_context.Proizvods, "ProizvodId", "Naziv", stavkanarudzbe.ProizvodId);
            return View(stavkanarudzbe);
        }

        // GET: Stavkanarudzbes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stavkanarudzbe = await _context.Stavkanarudzbes
                .Include(s => s.Narudzba)
                .Include(s => s.Proizvod)
                .FirstOrDefaultAsync(m => m.StavkaId == id);
            if (stavkanarudzbe == null)
            {
                return NotFound();
            }

            return View(stavkanarudzbe);
        }

        // POST: Stavkanarudzbes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stavkanarudzbe = await _context.Stavkanarudzbes.FindAsync(id);
            if (stavkanarudzbe != null)
            {
                _context.Stavkanarudzbes.Remove(stavkanarudzbe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StavkanarudzbeExists(int id)
        {
            return _context.Stavkanarudzbes.Any(e => e.StavkaId == id);
        }
    }
}
