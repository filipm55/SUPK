using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Narudzba
{
    public int NarudzbaId { get; set; }

    public DateTime VrijemeNarudzbe { get; set; }

    public int RacunId { get; set; }

    public virtual Racun Racun { get; set; } = null!;

    public virtual ICollection<Stavkanarudzbe> Stavkanarudzbes { get; set; } = new List<Stavkanarudzbe>();
}
