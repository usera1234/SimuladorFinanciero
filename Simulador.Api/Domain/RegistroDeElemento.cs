namespace Simulador.Api.Domain;

// Un evento ocurrido, con el tick en que pasó y la empresa afectada.
public record RegistroDeEvento(long Tick, string Empresa, string Evento, double ImpactoPorcentual);