using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
