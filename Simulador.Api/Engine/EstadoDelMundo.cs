namespace Simulador.Api.Engine;

// Foto del estado del mundo en un momento dado: tiempo y factores.
public record EstadoDelMundo(
    long Tick,
    double Velocidad,
    bool EstaPausado,
    IReadOnlyList<EstadoDeFactor> Factores);