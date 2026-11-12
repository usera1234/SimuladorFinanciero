namespace Simulador.Api.Domain;

// Instrumento en el que se puede invertir. Su precio cambia en cada tick
// según su rendimiento base, su sensibilidad a los factores y su azar propio.

public class Activo
{
    // Días por mes, para convertir rendimientos mensuales en diarios.
    public const double DiasPorMes = 30.0;

    // Rendimiento diario mínimo: evita que el precio llegue a cero o se vuelva negativo.
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

    // Rendimiento del último tick (0,01 = subió 1%). Lo usarán las empresas para su racha.
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
        PrecioActual = precioInicial;
        RendimientoBaseMensual = rendimientoBaseMensual;
        VolatilidadDiaria = volatilidadDiaria;
        _sensibilidades = new Dictionary<string, double>(sensibilidades); // Copia propia.
    }

    // Mueve el precio un tick según el valor actual de los factores.
    public void Avanzar(IReadOnlyDictionary<string, double> valoresDeFactores, Random azar)
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

        var rendimientoDiario = rendimientoMensual / 100.0 / DiasPorMes
                              + VolatilidadDiaria / 100.0 * azar.SiguienteNormal();

        AplicarRendimiento(rendimientoDiario);
    }

    // Cambia el precio según un rendimiento (0,05 = sube 5%). Protegido para que
    // las clases que hereden de Activo, como las empresas, puedan usarlo en sus eventos.
    protected void AplicarRendimiento(double rendimiento)
    {
        rendimiento = Math.Max(rendimiento, RendimientoDiarioMinimo);

        PrecioActual *= 1 + rendimiento;
        UltimoRendimiento = rendimiento;
    }
}