using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class EmpresaTests
{
    private static readonly Dictionary<string, double> SinFactores = new();

    private static readonly DefinicionDeEvento SubaFijaDeDiez =
        new("Suba fija", Peso: 1, ImpactoMinimo: 10, ImpactoMaximo: 10);

    private static Empresa CrearEmpresa(
        double probabilidadBaseDiaria,
        IEnumerable<DefinicionDeEvento>? eventos = null,
        double rendimientoBaseMensual = 0,
        double volatilidadDiaria = 0)
    {
        return new Empresa("Prueba", precioInicial: 100,
            rendimientoBaseMensual, volatilidadDiaria,
            new Dictionary<string, double>(),
            probabilidadBaseDiaria,
            eventos ?? new[] { SubaFijaDeDiez });
    }

    private static void AvanzarVariosTicks(Empresa empresa, int cantidad, Random azar)
    {
        for (var i = 0; i < cantidad; i++)
        {
            empresa.Avanzar(SinFactores, azar);
        }
    }

    [Fact]
    public void ConProbabilidadCero_NuncaTieneEventos()
    {
        var empresa = CrearEmpresa(probabilidadBaseDiaria: 0);

        AvanzarVariosTicks(empresa, 1000, new Random(1));

        Assert.Null(empresa.EventoDelUltimoTick);
        Assert.Equal(1000L, empresa.TicksSinEvento);
        Assert.Equal(100.0, empresa.PrecioActual, precision: 10);
    }

    [Fact]
    public void ConProbabilidadUno_TieneUnEventoEnCadaTick()
    {
        var empresa = CrearEmpresa(probabilidadBaseDiaria: 1);

        empresa.Avanzar(SinFactores, new Random(1));

        Assert.NotNull(empresa.EventoDelUltimoTick);
        Assert.Equal("Suba fija", empresa.EventoDelUltimoTick.Nombre);
        Assert.Equal(110.0, empresa.PrecioActual, precision: 10);
        Assert.Equal(0.1, empresa.UltimoRendimiento, precision: 10);
        Assert.Equal(0L, empresa.TicksSinEvento);
    }

    [Fact]
    public void CalcularProbabilidad_RecienCreada_EsLaProbabilidadBase()
    {
        var empresa = CrearEmpresa(probabilidadBaseDiaria: 0.004);

        Assert.Equal(0.004, empresa.CalcularProbabilidadDeEvento(), precision: 10);
    }

    [Fact]
    public void CalcularProbabilidad_TrasUnAnoSinEventos_SeDuplica()
    {
        // Probabilidad tan baja que en la práctica nunca ocurre un evento.
        const double probabilidadBase = 1e-12;
        var empresa = CrearEmpresa(probabilidadBase);

        AvanzarVariosTicks(empresa, 365, new Random(1));

        Assert.Equal(2.0, empresa.CalcularProbabilidadDeEvento() / probabilidadBase, precision: 10);
    }

    [Fact]
    public void CalcularProbabilidad_TrasDosAnosSinEventos_NoPasaDelDoble()
    {
        const double probabilidadBase = 1e-12;
        var empresa = CrearEmpresa(probabilidadBase);

        AvanzarVariosTicks(empresa, 730, new Random(1));

        Assert.Equal(2.0, empresa.CalcularProbabilidadDeEvento() / probabilidadBase, precision: 10);
    }

    [Fact]
    public void Racha_ConSubidaConstante_SeAcercaAlRendimientoDiario()
    {
        // 30% mensual = 1% diario. La racha debería acercarse a 0,01.
        var empresa = CrearEmpresa(probabilidadBaseDiaria: 0, rendimientoBaseMensual: 30);

        AvanzarVariosTicks(empresa, 200, new Random(1));

        Assert.InRange(empresa.Racha, 0.0099, 0.0100001);
    }

    [Fact]
    public void ConLaMismaSemilla_TieneLosMismosEventosYPrecios()
    {
        var eventos = new[]
        {
            new DefinicionDeEvento("Bueno", Peso: 1, ImpactoMinimo: 5, ImpactoMaximo: 20, InfluenciaDeLaRacha: 1),
            new DefinicionDeEvento("Malo", Peso: 1, ImpactoMinimo: -20, ImpactoMaximo: -5, InfluenciaDeLaRacha: -1),
        };
        var empresaA = CrearEmpresa(1.0 / 50, eventos, volatilidadDiaria: 3);
        var empresaB = CrearEmpresa(1.0 / 50, eventos, volatilidadDiaria: 3);

        AvanzarVariosTicks(empresaA, 2000, new Random(5));
        AvanzarVariosTicks(empresaB, 2000, new Random(5));

        Assert.Equal(empresaA.PrecioActual, empresaB.PrecioActual);
        Assert.Equal(empresaA.Racha, empresaB.Racha);
        Assert.Equal(empresaA.TicksSinEvento, empresaB.TicksSinEvento);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Constructor_ConProbabilidadFueraDeRango_LanzaExcepcion(double probabilidadInvalida)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearEmpresa(probabilidadInvalida));
    }

    [Fact]
    public void Constructor_ConProbabilidadYSinEventos_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearEmpresa(0.01, Array.Empty<DefinicionDeEvento>()));
    }

    [Fact]
    public void Constructor_ConEventoDePesoCero_LanzaExcepcion()
    {
        var evento = new DefinicionDeEvento("X", Peso: 0, ImpactoMinimo: 1, ImpactoMaximo: 2);

        Assert.Throws<ArgumentException>(() => CrearEmpresa(0.01, new[] { evento }));
    }

    [Fact]
    public void Constructor_ConImpactoMinimoMayorQueMaximo_LanzaExcepcion()
    {
        var evento = new DefinicionDeEvento("X", Peso: 1, ImpactoMinimo: 10, ImpactoMaximo: 5);

        Assert.Throws<ArgumentException>(() => CrearEmpresa(0.01, new[] { evento }));
    }

    [Fact]
    public void Constructor_ConInfluenciaFueraDeRango_LanzaExcepcion()
    {
        var evento = new DefinicionDeEvento("X", Peso: 1, ImpactoMinimo: 1, ImpactoMaximo: 2, InfluenciaDeLaRacha: 2);

        Assert.Throws<ArgumentException>(() => CrearEmpresa(0.01, new[] { evento }));
    }
}