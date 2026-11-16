namespace Simulador.Api.Engine;

// Foto completa de una partida: el mundo y la billetera, tomados en el mismo instante.
public record EstadoDePartida(EstadoDelMundo Mundo, EstadoDeBilletera Billetera);