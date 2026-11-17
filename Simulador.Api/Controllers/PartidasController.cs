using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Simulador.Api.Domain;
using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Permite crear partidas, controlar el paso del tiempo y operar en cada una.
[ApiController]
[Route("partidas")]
public class PartidasController : ControllerBase
{
    // Límites del parámetro "puntos" del historial.
    private const int MinimoDePuntos = 2;
    private const int MaximoDePuntos = 1000;

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

    // Devuelve la evolución de la partida para dibujar gráficos.
    // "puntos" es la cantidad máxima de días que se devuelven, por ejemplo /historial?puntos=300.
    [HttpGet("{id:guid}/historial")]
    public ActionResult<HistorialDePartida> ObtenerHistorial(Guid id, [FromQuery] int puntos = 300)
    {
        if (puntos < MinimoDePuntos || puntos > MaximoDePuntos)
        {
            return Problem(
                title: "Pedido inválido",
                detail: $"La cantidad de puntos debe estar entre {MinimoDePuntos} y {MaximoDePuntos}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        return partida.ObtenerHistorial(puntos);
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

        try
        {
            partida.Reanudar();
        }
        catch (OperacionInvalidaException error)
        {
            return Problem(title: "No se puede reanudar", detail: error.Message, statusCode: StatusCodes.Status400BadRequest);
        }

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

    // Compra un activo al precio actual.
    [HttpPost("{id:guid}/comprar")]
    public ActionResult<RespuestaDePartida> Comprar(Guid id, OperarRequest pedido)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        try
        {
            partida.Comprar(pedido.Activo, pedido.Cantidad);
        }
        catch (OperacionInvalidaException error)
        {
            return Problem(title: "Operación inválida", detail: error.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return ArmarRespuesta(partida);
    }

    // Vende un activo al precio actual.
    [HttpPost("{id:guid}/vender")]
    public ActionResult<RespuestaDePartida> Vender(Guid id, OperarRequest pedido)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        try
        {
            partida.Vender(pedido.Activo, pedido.Cantidad);
        }
        catch (OperacionInvalidaException error)
        {
            return Problem(title: "Operación inválida", detail: error.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return ArmarRespuesta(partida);
    }

    // Pasa efectivo a un plazo fijo. Cuerpo: { "monto": 100000, "dias": 30 }.
    [HttpPost("{id:guid}/plazos-fijos")]
    public ActionResult<RespuestaDePartida> ConstituirPlazoFijo(Guid id, PlazoFijoRequest pedido)
    {
        return EjecutarOperacion(id, partida => partida.ConstituirPlazoFijo(pedido.Monto, pedido.Dias));
    }

    // Cobra un plazo fijo vencido: capital más interés pasan al efectivo.
    [HttpPost("{id:guid}/plazos-fijos/{plazoFijoId:int}/cobrar")]
    public ActionResult<RespuestaDePartida> CobrarPlazoFijo(Guid id, int plazoFijoId)
    {
        return EjecutarOperacion(id, partida => partida.ResolverPlazoFijo(plazoFijoId, renovar: false));
    }

    // Renueva un plazo fijo vencido al mismo plazo, con capital más interés.
    [HttpPost("{id:guid}/plazos-fijos/{plazoFijoId:int}/renovar")]
    public ActionResult<RespuestaDePartida> RenovarPlazoFijo(Guid id, int plazoFijoId)
    {
        return EjecutarOperacion(id, partida => partida.ResolverPlazoFijo(plazoFijoId, renovar: true));
    }

    // Busca la partida, ejecuta la acción y convierte los errores del jugador en un 400.
    // Los métodos de los plazos fijos lo usan para no repetir el mismo try/catch tres veces.
    private ActionResult<RespuestaDePartida> EjecutarOperacion(Guid id, Action<Partida> accion)
    {
        var partida = _gestor.Obtener(id);
        if (partida is null)
        {
            return NotFound();
        }

        try
        {
            accion(partida);
        }
        catch (OperacionInvalidaException error)
        {
            return Problem(title: "Operación inválida", detail: error.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return ArmarRespuesta(partida);
    }

    private static RespuestaDePartida ArmarRespuesta(Partida partida)
    {
        var estado = partida.ObtenerEstado();
        return new RespuestaDePartida(partida.Id, partida.Semilla, estado.Mundo, estado.Billetera);
    }
}