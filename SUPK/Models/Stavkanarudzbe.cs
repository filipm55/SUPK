using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Stavkanarudzbe
{
    public int StavkaId { get; set; }

    public int Kolicina { get; set; }

    public int NarudzbaId { get; set; }

    public int ProizvodId { get; set; }

    public virtual Narudzba Narudzba { get; set; } = null!;

    public virtual Proizvod Proizvod { get; set; } = null!;
}
