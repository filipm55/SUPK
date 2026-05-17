using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using SUPK.Services;
using SUPK.ViewModels;
using Xunit;

namespace SUPK.Tests.Integration
{
    public class RacunIntegrationTests
    {
        //Testiranje kompletnog toka kreiranja računa, uključujući kreiranje računa, narudžbe i stavki narudžbe,
        //te provjeru da su svi entiteti ispravno spremljeni u bazu
        [Fact]
        public async Task Full_Create_Flow_Works()
        {
            
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CaffeBarDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new CaffeBarDbContext(options);
            await context.Database.EnsureCreatedAsync();


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
                        Kolicina = 2
                    }
                }
            };
            
            await service.CreateAsync(vm);

            Assert.Single(context.Racuns);
            Assert.Single(context.Narudzbas);
            Assert.Single(context.Stavkanarudzbes);
        }
    }
}