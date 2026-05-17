using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.Services;
using SUPK.ViewModels;
using Xunit;

namespace SUPK.Tests.Services
{
    public class RacunServiceTests
    {
        private CaffeBarDbContext CreateContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<CaffeBarDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new CaffeBarDbContext(options);
            context.Database.EnsureCreated();

            context.Konobars.Add(new Konobar
            {
                KonobarId = 1,
                Ime = "Ivan",
                Prezime = "Horvat",
                Aktivan = true
            });

            context.Stols.Add(new Stol
            {
                StolId = 1,
                BrojStola = 1,
            });

            context.Proizvods.Add(new Proizvod
            {
                ProizvodId = 1,
                Naziv = "Coca Cola",
                Cijena = 2.50m
            });

            context.SaveChanges();

            return context;
        }
        //Testiranje poslovne logike za kreiranje računa, narudžbe i stavke narudžbe
        [Fact]
        public async Task CreateAsync_CreatesRacunNarudzbaAndStavka()
        {
            
            var context = CreateContext();
            var service = new RacunService(context);

            var vm = new RacunCreateViewModel
            {
                Racun = new Racun
                {
                    VrijemeOtvaranja = DateTime.Now,
                    StolId = 1,
                    KonobarId = 1
                },
                Stavke =
                {
                    new StavkaViewModel
                    {
                        ProizvodId = 1,
                        Kolicina = 3
                    }
                }
            };

            
            await service.CreateAsync(vm);

            
            Assert.Equal(1, context.Racuns.Count());
            Assert.Equal(1, context.Narudzbas.Count());
            Assert.Equal(1, context.Stavkanarudzbes.Count());

            var racun = context.Racuns.First();
            Assert.Equal(7.50m, racun.UkupnaCijena);
        }
    }
}