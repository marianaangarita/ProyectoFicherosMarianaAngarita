using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Usuario
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RolId { get; set; }

    public bool? Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    public int? CreadoPorId { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public virtual ICollection<Auditorium> Auditoria { get; set; } = new List<Auditorium>();

    public virtual Usuario? CreadoPor { get; set; }

    public virtual ICollection<Usuario> InverseCreadoPor { get; set; } = new List<Usuario>();

    public virtual Rol Rol { get; set; } = null!;
}
