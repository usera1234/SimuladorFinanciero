namespace Simulador.Api.Domain;

// Instrumento en el que se puede invertir. Su precio cambia en cada tick
// según su rendimiento base, su sensibilidad a los factores y su azar propio.
public class Activo
{
    // Días por mes, para convertir rendimientos mensuales en diarios.
    public const double DiasPorMes = 30.0;

    // Rendimiento mínimo de un cambio de precio: evita que el precio llegue a cero o se vuelva negativo.
    public const double RendimientoDiarioMinimo = -0.99;

    private readonly Dictionary<string, double> _sensibilidades;

    public string Nombre { get; }
    public TipoDeActivo Tipo { get; }

    // Rendimiento propio del activo, en porcentaje mensual, sin contar los factores.
    public double RendimientoBaseMensual { get; }

    // Desvío típico del azar propio, en porcentaje diario.
    public double VolatilidadDiaria { get; }

    // Cuánto le afecta cada factor, por nombre de factor.
    public IReadOnlyDictionary<string, double> Sensibilidades => _sensibilidades;

    public double PrecioActual { get; private set; }

    public double PrecioInicial { get; }

    // Rendimiento total del último tick (0,01 = subió 1%), incluyendo mercado y eventos.
    public double UltimoRendimiento { get; private set; }

    // Si se puede comprar una parte de una unidad (0,5 Bitcoin) o solo unidades enteras.
    // Las acciones se compran por unidades enteras, como en la bolsa.
    public bool PermiteFracciones => Tipo != TipoDeActivo.Accion;

    public Activo(
        string nombre,
        TipoDeActivo tipo,
        double precioInicial,
        double rendimientoBaseMensual,
        double volatilidadDiaria,
        IDictionary<string, double> sensibilidades)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El activo necesita un nombre.", nameof(nombre));
        }

        if (precioInicial <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precioInicial), "El precio inicial debe ser mayor que cero.");
        }

        if (volatilidadDiaria < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(volatilidadDiaria), "La volatilidad no puede ser negativa.");
        }

        Nombre = nombre;
        Tipo = tipo;
        PrecioInicial = precioInicial;
        PrecioActual = precioInicial;
        RendimientoBaseMensual = rendimientoBaseMensual;
        VolatilidadDiaria = volatilidadDiaria;
        _sensibilidades = new Dictionary<string, double>(sensibilidades); // Copia propia.
    }

    // Mueve el precio un tick: primero el mercado, después lo propio de cada tipo de activo.
    public void Avanzar(IReadOnlyDictionary<string, double> valoresDeFactores, Random azar)
    {
        var precioAlInicioDelTick = PrecioActual;

        AplicarRendimiento(CalcularRendimientoDeMercado(valoresDeFactores, azar));
        AlTerminarElTick(precioAlInicioDelTick, azar);

        UltimoRendimiento = PrecioActual / precioAlInicioDelTick - 1;
    }

    // Lugar para que las clases que heredan agreguen comportamiento al final de cada tick.
    // En un activo común no hace nada.
    protected virtual void AlTerminarElTick(double precioAlInicioDelTick, Random azar)
    {
    }

    // Cambia el precio según un rendimiento (0,05 = sube 5%).
    protected void AplicarRendimiento(double rendimiento)
    {
        rendimiento = Math.Max(rendimiento, RendimientoDiarioMinimo);
        PrecioActual *= 1 + rendimiento;
    }

    // Rendimiento diario causado por el mercado: base, factores y azar propio.
    private double CalcularRendimientoDeMercado(IReadOnlyDictionary<string, double> valoresDeFactores, Random azar)
    {
        var rendimientoMensual = RendimientoBaseMensual;

        foreach (var (nombreDelFactor, sensibilidad) in _sensibilidades)
        {
            if (!valoresDeFactores.TryGetValue(nombreDelFactor, out var valor))
            {
                throw new InvalidOperationException(
                    $"El activo '{Nombre}' depende del factor '{nombreDelFactor}', que no existe en el mundo.");
            }

            rendimientoMensual += sensibilidad * valor;
        }

        return rendimientoMensual / 100.0 / DiasPorMes
             + VolatilidadDiaria / 100.0 * azar.SiguienteNormal();
    }
}