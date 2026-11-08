using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Pasajero
{
    public int Id { get; set; }

    public string DocumentoIdentidad { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string PrimerApellido { get; set; } = null!;

    public string? SegundoApellido { get; set; }

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
