using System.ComponentModel.DataAnnotations;
using SUPK.Models;

namespace SUPK.ViewModels
{
    public class RacunEditViewModel
    {
        public Racun Racun { get; set; } = new Racun();

        public List<NarudzbaEditViewModel> Narudzbe { get; set; } = new();
    }

    public class NarudzbaEditViewModel
    {
        public int? NarudzbaId { get; set; }
        
        public DateTime VrijemeNarudzbe { get; set; }

        public List<StavkaEditViewModel> Stavke { get; set; } = new();
    }

    public class StavkaEditViewModel
    {
        public int? StavkaId { get; set; }

        [Required(ErrorMessage = "Proizvod je obvezan za svaku stavku.")]
        public int? ProizvodId { get; set; }

        [Required(ErrorMessage = "Količina je obavezna.")]
        [Range(1, int.MaxValue, ErrorMessage = "Količina mora biti veća od 0.")]
        public int? Kolicina { get; set; }
    }
}
