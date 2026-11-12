using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Foto del estado del mundo en un momento dado: tiempo, factores, activos y últimos eventos.
public record EstadoDelMundo(
    long Tick,
    double Velocidad,
    bool EstaPausado,
    IReadOnlyList<EstadoDeFactor> Factores,
    IReadOnlyList<EstadoDeActivo> Activos,
    IReadOnlyList<RegistroDeEvento> UltimosEventos);