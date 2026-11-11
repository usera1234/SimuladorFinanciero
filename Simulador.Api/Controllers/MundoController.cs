using Microsoft.AspNetCore.Mvc;
using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Permite consultar y controlar el paso del tiempo en el mundo simulado.

[ApiController]

[Route("mundo")]

public class MundoController : ControllerBase

{

    private readonly RelojDeSimulacion _reloj;

    public MundoController(RelojDeSimulacion reloj)

    {

        _reloj = reloj;

    }

    // Devuelve el estado actual del mundo, puesto al día.

    [HttpGet]

    public ActionResult<EstadoDelMundo> ObtenerEstado()

    {

        return _reloj.ObtenerEstado();

    }

    // Pausa el avance del tiempo.

    [HttpPost("pausar")]

    public ActionResult<EstadoDelMundo> Pausar()

    {

        _reloj.Pausar();
        return _reloj.ObtenerEstado();

    }

    // Reanuda el avance del tiempo.
   
    [HttpPost("reanudar")]

    public ActionResult<EstadoDelMundo> Reanudar()

    {

        _reloj.Reanudar();
        return _reloj.ObtenerEstado();

    }

    // Cambia la velocidad de la simulación.
   
    [HttpPut("velocidad")]

    public ActionResult<EstadoDelMundo> CambiarVelocidad(CambiarVelocidadRequest pedido)

    {

        _reloj.CambiarVelocidad(pedido.Velocidad);
        return _reloj.ObtenerEstado();

    }

}