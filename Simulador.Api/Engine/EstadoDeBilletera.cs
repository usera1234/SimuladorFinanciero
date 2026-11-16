using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Foto de la billetera del jugador. Valor real: valor en pesos del inicio, descontando la inflación.
public record EstadoDeBilletera(
    decimal CapitalInicial,
    decimal Efectivo,
    IReadOnlyList<EstadoDePosicion> Posiciones,
    decimal ValorTotal,
    decimal GananciaNominalPorcentual,
    decimal ValorReal,
    decimal GananciaRealPorcentual,
    IReadOnlyList<Operacion> UltimasOperaciones);