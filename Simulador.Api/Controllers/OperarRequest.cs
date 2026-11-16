using System.ComponentModel.DataAnnotations;

namespace Simulador.Api.Controllers;

// Cuerpo de los pedidos de compra y venta: qué activo y cuánto.
public class OperarRequest
{
    [Required]
    public string Activo { get; set; } = "";

    public decimal Cantidad { get; set; }
}