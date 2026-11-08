using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Pai
{
    public string CodigoIso { get; set; } = null!;

    public virtual ICollection<Reserva> ReservaDestinoNavigations { get; set; } = new List<Reserva>();

    public virtual ICollection<Reserva> ReservaOrigenNavigations { get; set; } = new List<Reserva>();
}
