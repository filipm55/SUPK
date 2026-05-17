using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Konobar
{
    public int KonobarId { get; set; }

    public string Ime { get; set; } = null!;

    public string Prezime { get; set; } = null!;

    public bool? Aktivan { get; set; }

    public virtual ICollection<Racun> Racuns { get; set; } = new List<Racun>();
}
