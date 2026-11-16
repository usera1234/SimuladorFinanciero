using System.ComponentModel.DataAnnotations;
using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Datos para cambiar la velocidad de la simulación.

public class CambiarVelocidadRequest
{

    // Nueva velocidad. Debe ser mayor que 0 y menor o igual que la velocidad máxima.

    [Range(0, RelojDeSimulacion.VelocidadMaxima, MinimumIsExclusive = true)]
    public double Velocidad { get; set; }
}
