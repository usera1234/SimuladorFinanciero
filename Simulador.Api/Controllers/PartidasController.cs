using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Permite crear partidas y controlar el paso del tiempo en cada una.
[ApiController]
[Route("partidas")]
public class PartidasController : ControllerBase
{
    private readonly GestorDePartidas _gestor;

    public PartidasController(GestorDePartidas gestor)
    {
        _gestor = gestor;
    }

    // Crea una partida nueva, en pausa. El cuerpo es opcional.
    [HttpPost]
    public ActionResult<RespuestaDePartida> Crear(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CrearPartidaRequest? pedido)
    {
        var partida = _gestor.Crear(pedido?.Semilla);
        return CreatedAtAction(nameof(Obtener), new { id = partida.Id }, ArmarRespuesta(partida));
    }

    // Devuelve el estado actual de una partida.
    [HttpGet("{id:guid}")]
    public ActionResult<RespuestaDePartida> Obtener(Guid id)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        return ArmarRespuesta(partida);
    }

    [HttpPost("{id:guid}/pausar")]
    public ActionResult<RespuestaDePartida> Pausar(Guid id)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        partida.Reloj.Pausar();
        return ArmarRespuesta(partida);
    }

    [HttpPost("{id:guid}/reanudar")]
    public ActionResult<RespuestaDePartida> Reanudar(Guid id)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        partida.Reloj.Reanudar();
        return ArmarRespuesta(partida);
    }

    [HttpPut("{id:guid}/velocidad")]
    public ActionResult<RespuestaDePartida> CambiarVelocidad(Guid id, CambiarVelocidadRequest pedido)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        partida.Reloj.CambiarVelocidad(pedido.Velocidad);
        return ArmarRespuesta(partida);
    }

    private static RespuestaDePartida ArmarRespuesta(Partida partida)
    {
        return new RespuestaDePartida(partida.Id, partida.Semilla, partida.Reloj.ObtenerEstado());
    }
}