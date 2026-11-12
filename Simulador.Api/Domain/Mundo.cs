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

    public const int MaximoDeEventosGuardados = 50;

    private readonly List<RegistroDeEvento> _ultimosEventos = new();

    // Cuánto subieron los precios desde el inicio (1,25 = subieron 25%).
    // Sirve para calcular el valor real de una cartera, es decir, su poder de compra.
    public double IndiceDePrecios { get; private set; } = 1.0;

    // Crea un mundo vacío. Útil para probar solo el paso del tiempo.
    public Mundo(IEnumerable<Factor> factores, IEnumerable<Activo> activos, Random azar, string? factorDeInflacion = null)
    {
        _factores = factores.ToList();
        _activos = activos.ToList();
        _azar = azar;
        _factorDeInflacion = factorDeInflacion;

        ValidarConfiguracion();
    }

    // Crea un mundo con factores pero sin activos.
    public Mundo(IEnumerable<Factor> factores, Random azar) : this(factores, Array.Empty<Activo>(), azar)
    {
    }
    // Últimos eventos ocurridos, del más viejo al más nuevo.
    public IReadOnlyList<RegistroDeEvento> UltimosEventos => _ultimosEventos;

    // Nombre del factor que representa la inflación, o null si el mundo no tiene uno.
    private readonly string? _factorDeInflacion;

    // Cantidad máxima de eventos que se guardan; los más viejos se descartan.

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

            if (_factorDeInflacion is not null)
            {
                IndiceDePrecios *= 1 + valoresDeFactores[_factorDeInflacion] / 100.0 / Activo.DiasPorMes;
            }

            foreach (var activo in _activos)
            {
                activo.Avanzar(valoresDeFactores, _azar);
            }

            TickActual++;
            RegistrarEventos();
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

        if (_factorDeInflacion is not null && !nombresDeFactores.Contains(_factorDeInflacion))
        {
            throw new ArgumentException($"El factor de inflación '{_factorDeInflacion}' no existe.");
        }
    }
    // Guarda los eventos que ocurrieron en el tick que acaba de terminar.
    private void RegistrarEventos()
    {
        foreach (var activo in _activos)
        {
            if (activo is Empresa empresa && empresa.EventoDelUltimoTick is not null)
            {
                var evento = empresa.EventoDelUltimoTick;
                _ultimosEventos.Add(new RegistroDeEvento(TickActual, empresa.Nombre, evento.Nombre, evento.ImpactoPorcentual));
            }
        }

        if (_ultimosEventos.Count > MaximoDeEventosGuardados)
        {
            _ultimosEventos.RemoveRange(0, _ultimosEventos.Count - MaximoDeEventosGuardados);
        }
    }

}