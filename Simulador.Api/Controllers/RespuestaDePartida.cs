using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Lo que devuelve la API sobre una partida: su identificador, su semilla y el estado de su mundo.
public record RespuestaDePartida(Guid Id, int Semilla, EstadoDelMundo Mundo);