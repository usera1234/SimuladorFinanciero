using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Crea mundos con la configuración del mercado argentino del simulador.
// Valores provisorios, pendientes de calibrar (ver docs/fundamentacion-del-modelo.md).
public static class ConfiguracionDelMercado
{
    // Nombres de los factores, para no escribirlos dos veces y evitar errores de tipeo.
    public const string Inflacion = "Inflación mensual (%)";
    public const string Devaluacion = "Devaluación mensual (%)";
    public const string AnimoCripto = "Ánimo cripto";
    public const string Actividad = "Actividad económica";

    // Precio inicial del dólar en pesos. Se usa también para las criptomonedas.
    public const double PrecioInicialDelDolar = 1_450;

    // Crea un mundo nuevo. Con la misma semilla, el mercado se comporta siempre igual.
    public static Mundo CrearMundo(int semilla)
    {
        return new Mundo(CrearFactores(), CrearActivos(), new Random(semilla));
    }

    private static Factor[] CrearFactores()
    {
        return new[]
        {
            new Factor(Inflacion, valorNormal: 2.0, volatilidad: 0.1, fuerzaDeRetorno: 0.02),
            new Factor(Devaluacion, valorNormal: 1.0, volatilidad: 0.2, fuerzaDeRetorno: 0.03),
            new Factor(AnimoCripto, valorNormal: 0.0, volatilidad: 1.0, fuerzaDeRetorno: 0.05),
            new Factor(Actividad, valorNormal: 0.0, volatilidad: 0.2, fuerzaDeRetorno: 0.02),
        };
    }

    private static Activo[] CrearActivos()
    {
        return new Activo[]
        {
            // El precio del plazo fijo representa cuánto vale cada peso depositado.
            new Activo("Plazo fijo", TipoDeActivo.Ahorro, precioInicial: 1,
                rendimientoBaseMensual: 2.6, volatilidadDiaria: 0,
                sensibilidades: new Dictionary<string, double>()),

            new Activo("Dólar", TipoDeActivo.Moneda, precioInicial: PrecioInicialDelDolar,
                rendimientoBaseMensual: 0, volatilidadDiaria: 0.4,
                sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0 }),

            // Consolidada: pocos eventos (~1 por año) y de baja intensidad.
            new Empresa("Energía del Sur", precioInicial: 18_000,
                rendimientoBaseMensual: 0.3, volatilidadDiaria: 1.0,
                sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.9, [Actividad] = 0.5 },
                probabilidadBaseDiaria: 1.0 / 573,
                eventosPosibles: new[]
                {
                    new DefinicionDeEvento("Buen balance", Peso: 35, ImpactoMinimo: 2, ImpactoMaximo: 6, InfluenciaDeLaRacha: 1),
                    new DefinicionDeEvento("Balance flojo", Peso: 20, ImpactoMinimo: -6, ImpactoMaximo: -2, InfluenciaDeLaRacha: -1),
                    new DefinicionDeEvento("Ajuste de tarifas aprobado", Peso: 25, ImpactoMinimo: 3, ImpactoMaximo: 8),
                    new DefinicionDeEvento("Lanzamiento exitoso", Peso: 18, ImpactoMinimo: 2, ImpactoMaximo: 6, InfluenciaDeLaRacha: 1),
                    new DefinicionDeEvento("Lanzamiento fallido", Peso: 2, ImpactoMinimo: -4, ImpactoMaximo: -2, InfluenciaDeLaRacha: -1),
                }),

            // Nueva: muchos eventos (~3 por año), intensos para los dos lados.
            new Empresa("NubeTec", precioInicial: 850,
                rendimientoBaseMensual: 0.5, volatilidadDiaria: 3.0,
                sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.5, [Actividad] = 1.5 },
                probabilidadBaseDiaria: 1.0 / 161,
                eventosPosibles: new[]
                {
                    new DefinicionDeEvento("Lanzamiento exitoso", Peso: 25, ImpactoMinimo: 10, ImpactoMaximo: 30, InfluenciaDeLaRacha: 1),
                    new DefinicionDeEvento("Lanzamiento fallido", Peso: 25, ImpactoMinimo: -25, ImpactoMaximo: -10, InfluenciaDeLaRacha: -1),
                    new DefinicionDeEvento("Ronda de inversión", Peso: 15, ImpactoMinimo: 5, ImpactoMaximo: 15, InfluenciaDeLaRacha: 1),
                    new DefinicionDeEvento("Despidos", Peso: 20, ImpactoMinimo: -15, ImpactoMaximo: -5, InfluenciaDeLaRacha: -1),
                    new DefinicionDeEvento("Escándalo mediático", Peso: 15, ImpactoMinimo: -30, ImpactoMaximo: -10),
                }),

            // Agresiva: eventos medios (~2 por año), de muy alta intensidad.
            new Empresa("Constructora Ágil", precioInicial: 4_200,
                rendimientoBaseMensual: 0, volatilidadDiaria: 2.0,
                sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.8, [Actividad] = 3.0 },
                probabilidadBaseDiaria: 1.0 / 256,
                eventosPosibles: new[]
                {
                    new DefinicionDeEvento("Gana una licitación grande", Peso: 35, ImpactoMinimo: 10, ImpactoMaximo: 25, InfluenciaDeLaRacha: 1),
                    new DefinicionDeEvento("Pierde una licitación", Peso: 30, ImpactoMinimo: -20, ImpactoMaximo: -8, InfluenciaDeLaRacha: -1),
                    new DefinicionDeEvento("Problemas de deuda", Peso: 20, ImpactoMinimo: -30, ImpactoMaximo: -12, InfluenciaDeLaRacha: -1),
                    new DefinicionDeEvento("Escándalo mediático", Peso: 15, ImpactoMinimo: -25, ImpactoMaximo: -10),
                }),

            // Exportadora: eventos medios (~1,5 por año), ajenos a la empresa.
            new Empresa("AgroExporta", precioInicial: 9_500,
                rendimientoBaseMensual: 0.2, volatilidadDiaria: 1.5,
                sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.3, [Devaluacion] = 1.0, [Actividad] = 0.3 },
                probabilidadBaseDiaria: 1.0 / 365,
                eventosPosibles: new[]
                {
                    new DefinicionDeEvento("Cosecha récord", Peso: 30, ImpactoMinimo: 8, ImpactoMaximo: 18),
                    new DefinicionDeEvento("Sequía", Peso: 30, ImpactoMinimo: -20, ImpactoMaximo: -8),
                    new DefinicionDeEvento("Baja de retenciones", Peso: 20, ImpactoMinimo: 5, ImpactoMaximo: 12),
                    new DefinicionDeEvento("Suba de retenciones", Peso: 20, ImpactoMinimo: -12, ImpactoMaximo: -5),
                }),

            // Cripto: precio en dólares × precio inicial del dólar.
            new Activo("Bitcoin", TipoDeActivo.Cripto, precioInicial: 76_000 * PrecioInicialDelDolar,
                rendimientoBaseMensual: 0, volatilidadDiaria: 3.0,
                sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0, [AnimoCripto] = 1.0 }),

            new Activo("Ethereum", TipoDeActivo.Cripto, precioInicial: 2_000 * PrecioInicialDelDolar,
                rendimientoBaseMensual: 0, volatilidadDiaria: 4.5,
                sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0, [AnimoCripto] = 1.5 }),
        };
    }
}