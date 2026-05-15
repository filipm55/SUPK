using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.ViewModels;

namespace SUPK.Services
{
    public class RacunService : IRacunService
    {
        private readonly CaffeBarDbContext _context;

        public RacunService(CaffeBarDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(RacunCreateViewModel vm)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Spremi Racun
                _context.Racuns.Add(vm.Racun);
                await _context.SaveChangesAsync();

                // reiraj Narudzbu
                var narudzba = new Narudzba
                {
                    RacunId = vm.Racun.RacunId,
                    VrijemeNarudzbe = DateTime.Now
                };

                _context.Narudzbas.Add(narudzba);
                await _context.SaveChangesAsync();

                // Spremi stavke
                foreach (var stavkaVm in vm.Stavke)
                {
                    if (stavkaVm.ProizvodId.HasValue &&
                        stavkaVm.Kolicina.HasValue &&
                        stavkaVm.Kolicina.Value > 0)
                    {
                        _context.Stavkanarudzbes.Add(new Stavkanarudzbe
                        {
                            NarudzbaId = narudzba.NarudzbaId,
                            ProizvodId = stavkaVm.ProizvodId.Value,
                            Kolicina = stavkaVm.Kolicina.Value
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Izračun ukupne cijene
                var total = await _context.Stavkanarudzbes
                    .Where(s => s.NarudzbaId == narudzba.NarudzbaId)
                    .Include(s => s.Proizvod)
                    .Select(s => (decimal?)s.Kolicina * s.Proizvod.Cijena)
                    .SumAsync();

                vm.Racun.UkupnaCijena = total ?? 0m;
                _context.Racuns.Update(vm.Racun);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(RacunEditViewModel vm)
        {
            vm.Narudzbe ??= new(); 

            var racunFromDb = await _context.Racuns
                .Include(r => r.Narudzbas)
                    .ThenInclude(n => n.Stavkanarudzbes)
                .FirstOrDefaultAsync(r => r.RacunId == vm.Racun.RacunId);

            if (racunFromDb == null)
                throw new DbUpdateConcurrencyException("Račun nije pronađen.");

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
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}