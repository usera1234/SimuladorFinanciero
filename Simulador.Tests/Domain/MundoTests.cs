using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class MundoTests
{
    [Fact]
    public void MundoNuevo_EmpiezaEnTickCero()
    {
        var mundo = new Mundo();

        Assert.Equal(0L, mundo.TickActual);
    }

    [Fact]
    public void AvanzarTicks_SumaLaCantidadIndicada()
    {
        var mundo = new Mundo();

        mundo.AvanzarTicks(5);
        mundo.AvanzarTicks(3);

        Assert.Equal(8L, mundo.TickActual);
    }

    [Fact]
    public void AvanzarTicks_ConCero_NoCambiaElTick()
    {
        var mundo = new Mundo();

        mundo.AvanzarTicks(0);

        Assert.Equal(0L, mundo.TickActual);
    }

    [Fact]
    public void AvanzarTicks_ConNegativo_LanzaExcepcion()
    {
        var mundo = new Mundo();

        Assert.Throws<ArgumentOutOfRangeException>(() => mundo.AvanzarTicks(-1));
    }

    [Fact]
    public void AvanzarTicks_MueveLosFactoresUnaVezPorTick()
    {
        var factor = new Factor("Prueba", valorNormal: 100, volatilidad: 0, fuerzaDeRetorno: 0.1, valorInicial: 110);
        var mundo = new Mundo(new[] { factor }, new Random(1));

        mundo.AvanzarTicks(2);

        Assert.Equal(2L, mundo.TickActual);
        Assert.Equal(108.1, mundo.Factores[0].ValorActual, precision: 10);
    }

    [Fact]
    public void AvanzarTicks_ConLaMismaSemilla_EsReproducible()
    {
        var mundoA = new Mundo(new[] { new Factor("A", 2, 0.5, 0.05) }, new Random(7));
        var mundoB = new Mundo(new[] { new Factor("A", 2, 0.5, 0.05) }, new Random(7));

        mundoA.AvanzarTicks(365);
        mundoB.AvanzarTicks(365);

        Assert.Equal(mundoA.Factores[0].ValorActual, mundoB.Factores[0].ValorActual);
    }

    [Fact]
    public void AvanzarTicks_RegistraLosEventosConSuTick()
    {
        var empresa = new Empresa("Prueba", 100, 0, 0, new Dictionary<string, double>(),
            probabilidadBaseDiaria: 1,
            eventosPosibles: new[] { new DefinicionDeEvento("Suba", Peso: 1, ImpactoMinimo: 10, ImpactoMaximo: 10) });
        var mundo = new Mundo(Array.Empty<Factor>(), new Activo[] { empresa }, new Random(1));

        mundo.AvanzarTicks(3);

        Assert.Equal(3, mundo.UltimosEventos.Count);
        Assert.Equal(new long[] { 1, 2, 3 }, mundo.UltimosEventos.Select(registro => registro.Tick));
        Assert.All(mundo.UltimosEventos, registro => Assert.Equal("Prueba", registro.Empresa));
    }

    [Fact]
    public void UltimosEventos_NoSuperaElMaximo()
    {
        var empresa = new Empresa("Prueba", 100, 0, 0, new Dictionary<string, double>(),
            probabilidadBaseDiaria: 1,
            eventosPosibles: new[] { new DefinicionDeEvento("Nada", Peso: 1, ImpactoMinimo: 0, ImpactoMaximo: 0) });
        var mundo = new Mundo(Array.Empty<Factor>(), new Activo[] { empresa }, new Random(1));

        mundo.AvanzarTicks(60);

        Assert.Equal(Mundo.MaximoDeEventosGuardados, mundo.UltimosEventos.Count);
        Assert.Equal(11L, mundo.UltimosEventos[0].Tick);
        Assert.Equal(60L, mundo.UltimosEventos[^1].Tick);
    }

    [Fact]
    public void Constructor_ConActivoQueDependeDeFactorInexistente_LanzaExcepcion()
    {
        var activo = new Activo("Prueba", TipoDeActivo.Moneda, 100, 0, 0,
            new Dictionary<string, double> { ["No existe"] = 1 });

        Assert.Throws<ArgumentException>(() =>
            new Mundo(Array.Empty<Factor>(), new[] { activo }, new Random(1)));
    }

    [Fact]
    public void Constructor_ConActivosRepetidos_LanzaExcepcion()
    {
        var activoA = new Activo("Repetido", TipoDeActivo.Moneda, 100, 0, 0, new Dictionary<string, double>());
        var activoB = new Activo("Repetido", TipoDeActivo.Moneda, 100, 0, 0, new Dictionary<string, double>());

        Assert.Throws<ArgumentException>(() =>
            new Mundo(Array.Empty<Factor>(), new[] { activoA, activoB }, new Random(1)));
    }

    [Fact]
    public void IndiceDePrecios_SinFactorDeInflacion_QuedaEnUno()
    {
        var mundo = new Mundo(new[] { new Factor("X", 3, 0, 0.1) }, new Random(1));

        mundo.AvanzarTicks(30);

        Assert.Equal(1.0, mundo.IndiceDePrecios);
    }

    [Fact]
    public void IndiceDePrecios_ConInflacionFija_SeAcumulaCadaTick()
    {
        // Inflación fija de 3% mensual = 0,1% diario.
        var inflacion = new Factor("Inflación", valorNormal: 3, volatilidad: 0, fuerzaDeRetorno: 0.1);
        var mundo = new Mundo(new[] { inflacion }, Array.Empty<Activo>(), new Random(1), factorDeInflacion: "Inflación");

        mundo.AvanzarTicks(2);

        Assert.Equal(1.001 * 1.001, mundo.IndiceDePrecios, precision: 10);
    }

    [Fact]
    public void Constructor_ConFactorDeInflacionInexistente_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new Mundo(Array.Empty<Factor>(), Array.Empty<Activo>(), new Random(1), factorDeInflacion: "No existe"));
    }
}