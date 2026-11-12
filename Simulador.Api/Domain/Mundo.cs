namespace Simulador.Api.Domain;

// Estado del mundo simulado: en qué tick está, cómo están los factores de la economía
// y cuánto valen los activos. No conoce el tiempo real.
public class Mundo
{
    private readonly List<Factor> _factores;
    private readonly List<Activo> _activos;
    private readonly Random _azar;

    public long TickActual { get; private set; }

    // Factores de la economía. Se pueden leer, pero no agregar ni quitar desde afuera.
    public IReadOnlyList<Factor> Factores => _factores;

    // Activos en los que se puede invertir. Se pueden leer, pero no agregar ni quitar desde afuera.
    public IReadOnlyList<Activo> Activos => _activos;

    // Crea un mundo vacío. Útil para probar solo el paso del tiempo.
    public Mundo() : this(Array.Empty<Factor>(), Array.Empty<Activo>(), new Random())
    {
    }

    // Crea un mundo con factores pero sin activos.
    public Mundo(IEnumerable<Factor> factores, Random azar) : this(factores, Array.Empty<Activo>(), azar)
    {
    }

    public Mundo(IEnumerable<Factor> factores, IEnumerable<Activo> activos, Random azar)
    {
        _factores = factores.ToList();
        _activos = activos.ToList();
        _azar = azar;

        ValidarConfiguracion();
    }

    // Avanza la simulación la cantidad de ticks indicada.
    // En cada tick se mueven primero los factores y después los activos.
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

            var valoresDeFactores = _factores.ToDictionary(factor => factor.Nombre, factor => factor.ValorActual);

            foreach (var activo in _activos)
            {
                activo.Avanzar(valoresDeFactores, _azar);
            }

            TickActual++;
        }
    }

    // Verifica que no haya nombres repetidos y que cada activo dependa solo de factores existentes.
    private void ValidarConfiguracion()
    {
        var nombresDeFactores = new HashSet<string>();
        foreach (var factor in _factores)
        {
            if (!nombresDeFactores.Add(factor.Nombre))
            {
                throw new ArgumentException($"Hay dos factores llamados '{factor.Nombre}'.");
            }
        }

        var nombresDeActivos = new HashSet<string>();
        foreach (var activo in _activos)
        {
            if (!nombresDeActivos.Add(activo.Nombre))
            {
                throw new ArgumentException($"Hay dos activos llamados '{activo.Nombre}'.");
            }

            foreach (var nombreDelFactor in activo.Sensibilidades.Keys)
            {
                if (!nombresDeFactores.Contains(nombreDelFactor))
                {
                    throw new ArgumentException(
                        $"El activo '{activo.Nombre}' depende del factor '{nombreDelFactor}', que no existe.");
                }
            }
        }
    }
}