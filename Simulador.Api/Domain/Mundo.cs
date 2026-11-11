namespace Simulador.Api.Domain;

// Estado del mundo simulado: en qué tick está y cómo están los factores de la economía.
// No conoce el tiempo real.

public class Mundo

{

    private readonly List<Factor> _factores;
    private readonly Random _azar;

    public long TickActual { get; private set; }

    // Factores de la economía. Se pueden leer, pero no agregar ni quitar desde afuera.

    public IReadOnlyList<Factor> Factores => _factores;

    // Crea un mundo sin factores. Útil para probar solo el paso del tiempo.

    public Mundo() : this(Array.Empty<Factor>(), new Random())

    {

    }

    public Mundo(IEnumerable<Factor> factores, Random azar)

    {

        _factores = factores.ToList();
        _azar = azar;

    }

    // Avanza la simulación la cantidad de ticks indicada, moviendo los factores en cada uno.

    public void AvanzarTicks(long cantidad)

    {

        if (cantidad < 0)

        {

            throw new ArgumentOutOfRangeException(
                nameof(cantidad),
                "La cantidad de ticks no puede ser negativa.");

        }

        for (long i = 0; i < cantidad; i++)

        {

            foreach (var factor in _factores)

            {

                factor.Avanzar(_azar);

            }

            TickActual++;
        }
    }
}