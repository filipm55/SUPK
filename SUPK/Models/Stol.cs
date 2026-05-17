using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Stol
{
    public int StolId { get; set; }

    public short BrojStola { get; set; }

    public virtual ICollection<Pozivkonobara> Pozivkonobaras { get; set; } = new List<Pozivkonobara>();

    public virtual ICollection<Racun> Racuns { get; set; } = new List<Racun>();
}
