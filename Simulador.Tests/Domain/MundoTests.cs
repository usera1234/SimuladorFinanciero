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
}