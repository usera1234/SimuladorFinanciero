namespace Simulador.Api.Controllers;

// Cuerpo del pedido para constituir un plazo fijo: cuánta plata y a cuántos días.
public class PlazoFijoRequest
{
    public decimal Monto { get; set; }

    public int Dias { get; set; }
}