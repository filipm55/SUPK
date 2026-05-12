using System.ComponentModel.DataAnnotations;

namespace SUPK.Models;

public partial class Racun
{
    public int RacunId { get; set; }

    [Required(ErrorMessage = "Vrijeme otvaranja je obavezno.")]
    [DataType(DataType.DateTime)]
    public DateTime VrijemeOtvaranja { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? VrijemeZatvaranja { get; set; }
    
    public TipPlacanja? NacinPlacanja { get; set; }

    public decimal? UkupnaCijena { get; set; }

    [Required(ErrorMessage = "Molimo odaberite stol.")]
    public int? StolId { get; set; }

    [Required(ErrorMessage = "Molimo odaberite konobara.")]
    public int? KonobarId { get; set; }

    public virtual Konobar Konobar { get; set; } = null!;

    public virtual ICollection<Narudzba> Narudzbas { get; set; } = new List<Narudzba>();

    public virtual Stol Stol { get; set; } = null!;
}
