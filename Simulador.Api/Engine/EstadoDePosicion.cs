namespace Simulador.Api.Engine;

// Lo que tiene el jugador de un activo: cantidad, precio actual y valor en pesos.
public record EstadoDePosicion(string Activo, decimal Cantidad, decimal PrecioActual, decimal Valor);