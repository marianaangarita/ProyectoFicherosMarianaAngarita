using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Reserva
{
    public int Id { get; set; }

    public string Localizador { get; set; } = null!;

    public string Origen { get; set; } = null!;

    public string Destino { get; set; } = null!;

    public DateTime FechaReserva { get; set; }

    public virtual Pai DestinoNavigation { get; set; } = null!;

    public virtual Pai OrigenNavigation { get; set; } = null!;

    public virtual ICollection<Pasajero> Pasajeros { get; set; } = new List<Pasajero>();
}
