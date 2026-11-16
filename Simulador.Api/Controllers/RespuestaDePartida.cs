using Simulador.Api.Engine;

namespace Simulador.Api.Controllers;

// Lo que devuelve la API sobre una partida: su identificador, su semilla, el mundo y la billetera.
public record RespuestaDePartida(Guid Id, int Semilla, EstadoDelMundo Mundo, EstadoDeBilletera Billetera);