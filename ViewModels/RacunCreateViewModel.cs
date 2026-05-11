using System.ComponentModel.DataAnnotations;
using SUPK.Models;

namespace SUPK.ViewModels
{
    public class RacunCreateViewModel
    {
        public Racun Racun { get; set; } = new Racun();

        public List<StavkaViewModel> Stavke { get; set; } = new();
    }

    public class StavkaViewModel
    {
        [Required(ErrorMessage = "Proizvod je obvezan za svaku stavku.")]
        public int? ProizvodId { get; set; }

        [Required(ErrorMessage = "Količina je obavezna.")]
        [Range(1, int.MaxValue, ErrorMessage = "Količina mora biti veća od 0.")]
        public int? Kolicina { get; set; }
    }
}