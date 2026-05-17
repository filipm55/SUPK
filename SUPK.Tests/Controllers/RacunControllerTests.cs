using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SUPK.Controllers;
using SUPK.Models;
using SUPK.Services;
using SUPK.ViewModels;
using Xunit;

namespace SUPK.Tests.Controllers
{
    public class RacunControllerTests
    {
        //Testiranje kontrolera za kreiranje računa, provjera validacije modela i redirekcije nakon uspješnog kreiranja računa
        [Fact]
        public async Task Create_ValidModel_RedirectsToHomeIndex()
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
                BrojStola = 1
            });

            context.Proizvods.Add(new Proizvod
            {
                ProizvodId = 1,
                Naziv = "Coca Cola",
                Cijena = 2.50m
            });

            await context.SaveChangesAsync();

            var controller = new RacunsController(context, new RacunService(context));

            var now = DateTime.Now;
            var vm = new RacunCreateViewModel
            {
                Racun = new Racun
                {
                    VrijemeOtvaranja = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
                    StolId = 1,
                    KonobarId = 1
                },
                Stavke =
                {
                    new StavkaViewModel { ProizvodId = 1, Kolicina = 2 }
                }
            };

            var result = await controller.Create(vm);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);

            var racun = await context.Racuns.SingleAsync();
            Assert.Equal(5.00m, racun.UkupnaCijena);
            Assert.Null(racun.VrijemeZatvaranja);
            Assert.Null(racun.NacinPlacanja);
        }

        [Fact]
        public async Task Create_DuplicateProductInStavke_ReturnsViewWithModelError()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CaffeBarDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new CaffeBarDbContext(options);
            await context.Database.EnsureCreatedAsync();

            context.Konobars.Add(new Konobar { KonobarId = 1, Ime = "Ivan", Prezime = "Horvat", Aktivan = true });
            context.Stols.Add(new Stol { StolId = 1, BrojStola = 1 });
            context.Proizvods.Add(new Proizvod { ProizvodId = 1, Naziv = "Coca Cola", Cijena = 2.50m });
            await context.SaveChangesAsync();

            var controller = new RacunsController(context, new RacunService(context));

            var now = DateTime.Now;
            var vm = new RacunCreateViewModel
            {
                Racun = new Racun
                {
                    VrijemeOtvaranja = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
                    StolId = 1,
                    KonobarId = 1
                },
                Stavke =
                {
                    new StavkaViewModel { ProizvodId = 1, Kolicina = 1 },
                    new StavkaViewModel { ProizvodId = 1, Kolicina = 2 }
                }
            };

            var result = await controller.Create(vm);

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<RacunCreateViewModel>(view.Model);
            Assert.True(controller.ModelState.ContainsKey(""));
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("isti proizvod", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Create_TableAlreadyHasOpenRacun_ReturnsViewWithModelError()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CaffeBarDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new CaffeBarDbContext(options);
            await context.Database.EnsureCreatedAsync();

            context.Konobars.Add(new Konobar { KonobarId = 1, Ime = "Ivan", Prezime = "Horvat", Aktivan = true });
            context.Stols.Add(new Stol { StolId = 1, BrojStola = 1 });
            context.Proizvods.Add(new Proizvod { ProizvodId = 1, Naziv = "Coca Cola", Cijena = 2.50m });

            context.Racuns.Add(new Racun
            {
                RacunId = 10,
                VrijemeOtvaranja = DateTime.Now.AddHours(-1),
                StolId = 1,
                KonobarId = 1,
                UkupnaCijena = 0m,
                VrijemeZatvaranja = null,
                NacinPlacanja = null
            });
            await context.SaveChangesAsync();

            var controller = new RacunsController(context, new RacunService(context));

            var now = DateTime.Now;
            var vm = new RacunCreateViewModel
            {
                Racun = new Racun
                {
                    VrijemeOtvaranja = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
                    StolId = 1,
                    KonobarId = 1
                },
                Stavke =
                {
                    new StavkaViewModel { ProizvodId = 1, Kolicina = 1 }
                }
            };

            var result = await controller.Create(vm);

            var view = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("Racun.StolId"));
            Assert.Contains(controller.ModelState["Racun.StolId"].Errors, e => e.ErrorMessage.Contains("otvoren", StringComparison.OrdinalIgnoreCase));
        }
    }
}
