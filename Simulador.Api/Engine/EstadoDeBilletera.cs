using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Foto de la billetera del jugador. Valor real: valor en pesos del inicio, descontando la inflación.
// ValorTotal incluye los plazos fijos con el interés ganado hasta hoy.
// InflacionDelUltimoAnioPorcentual: la referencia con la que se calculan las tasas de plazo fijo que se ofrecen hoy.
public record EstadoDeBilletera(
    decimal CapitalInicial,
    decimal Efectivo,
    IReadOnlyList<EstadoDePosicion> Posiciones,
    decimal ValorTotal,
    decimal GananciaNominalPorcentual,
    decimal ValorReal,
    decimal GananciaRealPorcentual,
    IReadOnlyList<Operacion> UltimasOperaciones,
    IReadOnlyList<EstadoDePlazoFijo> PlazosFijos,
    IReadOnlyList<OpcionDePlazoFijo> OpcionesDePlazoFijo,
    double InflacionDelUltimoAnioPorcentual);

// Foto de un plazo fijo sin cobrar. Vencido: ya venció y espera que el jugador decida.
public record EstadoDePlazoFijo(
    int Id,
    decimal Capital,
    decimal TasaNominalAnual,
    int Dias,
    long TickDeInicio,
    long TickDeVencimiento,
    decimal InteresAcumulado,
    decimal ValorAlVencer,
    bool Vencido);

// Un plazo que se puede elegir y su tasa, para que la interfaz no tenga que repetir la tabla.
public record OpcionDePlazoFijo(int Dias, decimal TasaNominalAnual);