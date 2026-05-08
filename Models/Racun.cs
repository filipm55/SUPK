using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Racun
{
    public int RacunId { get; set; }

    public DateTime VrijemeOtvaranja { get; set; }

    public DateTime? VrijemeZatvaranja { get; set; }

    public decimal? UkupnaCijena { get; set; }

    public int StolId { get; set; }

    public int KonobarId { get; set; }

    public virtual Konobar Konobar { get; set; } = null!;

    public virtual ICollection<Narudzba> Narudzbas { get; set; } = new List<Narudzba>();

    public virtual Stol Stol { get; set; } = null!;
}
