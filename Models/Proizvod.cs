using System;
using System.Collections.Generic;

namespace SUPK.Models;

public partial class Proizvod
{
    public int ProizvodId { get; set; }

    public string Naziv { get; set; } = null!;

    public decimal Cijena { get; set; }

    public virtual ICollection<Stavkanarudzbe> Stavkanarudzbes { get; set; } = new List<Stavkanarudzbe>();
}
