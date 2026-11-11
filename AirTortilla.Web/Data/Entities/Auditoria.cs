using System;
using System.Collections.Generic;

namespace AirTortilla.Web.Data.Entities;

public partial class Auditoria
{
    public long Id { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime Fecha { get; set; }

    public string Accion { get; set; } = null!;

    public string? Tabla { get; set; }

    public string? RegistroId { get; set; }

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
