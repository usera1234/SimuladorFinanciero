namespace Simulador.Api.Domain;

// Un evento que efectivamente ocurrió, con el impacto que tuvo (en porcentaje).
public record EventoOcurrido(string Nombre, double ImpactoPorcentual);