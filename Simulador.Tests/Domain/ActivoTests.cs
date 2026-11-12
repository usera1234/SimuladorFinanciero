using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class ActivoTests
{
    private static readonly Dictionary<string, double> SinFactores = new();

    private static Activo CrearActivo(
        double rendimientoBaseMensual = 0,
        double volatilidadDiaria = 0,
        Dictionary<string, double>? sensibilidades = null,
        TipoDeActivo tipo = TipoDeActivo.Moneda,
        double precioInicial = 100)
    {
        return new Activo("Prueba", tipo, precioInicial, rendimientoBaseMensual, volatilidadDiaria,
            sensibilidades ?? new Dictionary<string, double>());
    }

    [Fact]
    public void Avanzar_SinFactoresNiAzar_CreceSegunElRendimientoBase()
    {
        // 3% mensual = 0,1% diario (3 / 100 / 30).
        var activo = CrearActivo(rendimientoBaseMensual: 3);

        activo.Avanzar(SinFactores, new Random(1));

        Assert.Equal(100.1, activo.PrecioActual, precision: 10);
        Assert.Equal(0.001, activo.UltimoRendimiento, precision: 10);
    }

    [Fact]
    public void Avanzar_ConSensibilidad_UsaElValorDelFactor()
    {
        // Sensibilidad 1 × factor 3 = 3% mensual = 0,1% diario.
        var activo = CrearActivo(sensibilidades: new Dictionary<string, double> { ["F"] = 1.0 });
        var valores = new Dictionary<string, double> { ["F"] = 3.0 };

        activo.Avanzar(valores, new Random(1));

        Assert.Equal(100.1, activo.PrecioActual, precision: 10);
    }

    [Fact]
    public void Avanzar_ConFactorInexistente_LanzaExcepcion()
    {
        var activo = CrearActivo(sensibilidades: new Dictionary<string, double> { ["No existe"] = 1.0 });

        Assert.Throws<InvalidOperationException>(() => activo.Avanzar(SinFactores, new Random(1)));
    }

    [Fact]
    public void Avanzar_ConCaidaExtrema_NoBajaDelMinimo()
    {
        // Un rendimiento absurdo de -100.000% mensual se limita a -99% diario.
        var activo = CrearActivo(rendimientoBaseMensual: -100_000);

        activo.Avanzar(SinFactores, new Random(1));

        Assert.Equal(1.0, activo.PrecioActual, precision: 10);
    }

    [Theory]
    [InlineData(TipoDeActivo.Ahorro, true)]
    [InlineData(TipoDeActivo.Moneda, true)]
    [InlineData(TipoDeActivo.Cripto, true)]
    [InlineData(TipoDeActivo.Accion, false)]
    public void PermiteFracciones_DependeDelTipo(TipoDeActivo tipo, bool esperado)
    {
        var activo = CrearActivo(tipo: tipo);

        Assert.Equal(esperado, activo.PermiteFracciones);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ConPrecioNoPositivo_LanzaExcepcion(double precioInvalido)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearActivo(precioInicial: precioInvalido));
    }

    [Fact]
    public void Constructor_ConVolatilidadNegativa_LanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearActivo(volatilidadDiaria: -1));
    }
}