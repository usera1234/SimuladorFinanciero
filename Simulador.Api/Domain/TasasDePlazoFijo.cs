namespace Simulador.Api.Domain;

// Calcula la tasa del plazo fijo como lo haría un banco que mira la inflación del último año:
// la convierte en una tasa equivalente para el plazo elegido y le suma un pequeño premio.
// A más días, más premio: se gana un poco más a cambio de tener la plata inmovilizada más tiempo.
public static class TasasDePlazoFijo
{
    // Premio por plazo, en tasa anual (0,005 = medio punto). Valores estimados, pendientes de calibrar.
    public static readonly IReadOnlyDictionary<int, decimal> PremioPorDias = new Dictionary<int, decimal>
    {
        [30] = 0.005m,
        [90] = 0.010m,
        [180] = 0.015m,
        [365] = 0.020m,
    };

    // Plazos que se pueden elegir.
    public static IEnumerable<int> PlazosDisponibles => PremioPorDias.Keys;

    // Tasa nominal anual para un plazo, a partir de la inflación anual (0,268 = 26,8 %).
    // Primero calcula qué tasa simple empataría con la inflación en ese plazo, y después le suma el premio.
    public static decimal Calcular(int dias, double inflacionAnual)
    {
        if (!PremioPorDias.TryGetValue(dias, out var premio))
        {
            throw new OperacionInvalidaException($"El plazo debe ser de {string.Join(", ", PlazosDisponibles)} días.");
        }

        // Inflación que se acumula en esos días, pasada a tasa anual simple (como cotizan los bancos).
        var inflacionDelPlazo = Math.Pow(1 + inflacionAnual, dias / 365.0) - 1;
        var tasaQueEmpata = inflacionDelPlazo * 365 / dias;

        // Nunca menos de 1 % anual, por si en algún momento hubiera deflación.
        var tasa = Math.Max((decimal)tasaQueEmpata + premio, 0.01m);
        return Math.Round(tasa, 4);
    }
}