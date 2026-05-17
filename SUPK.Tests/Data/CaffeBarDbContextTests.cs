using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using Xunit;

namespace SUPK.Tests.Data
{
    public class CaffeBarDbContextTests
    {
        // Testiranje osnovne funkcionalnosti DbContexta, provjera da se entiteti mogu dodavati i spremati u bazu
        [Fact]
        public void Can_Insert_Proizvod()
        {
            
            var options = new DbContextOptionsBuilder<CaffeBarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new CaffeBarDbContext(options);

            
            context.Proizvods.Add(new Proizvod
            {
                Naziv = "Espresso",
                Cijena = 1.80m
            });

            context.SaveChanges();

            
            Assert.Equal(1, context.Proizvods.Count());
        }
    }
}