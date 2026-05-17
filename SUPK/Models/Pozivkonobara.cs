using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Pozivkonobara
{
    public int PozivId { get; set; }

    public DateTime VrijemePoziva { get; set; }

    public int StolId { get; set; }

    public virtual Stol Stol { get; set; } = null!;
}
