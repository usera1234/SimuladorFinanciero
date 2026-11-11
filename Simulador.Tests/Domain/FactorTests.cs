using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class FactorTests
{
    [Fact]
    public void NuevoFactor_SinValorInicial_ArrancaEnSuValorNormal()
    {
        var factor = new Factor("Prueba", valorNormal: 100, volatilidad: 1, fuerzaDeRetorno: 0.1);

        Assert.Equal(100.0, factor.ValorActual);
    }

    [Fact]
    public void Avanzar_SinVolatilidad_SeAcercaAlValorNormalSegunLaFuerza()
    {
        var factor = new Factor("Prueba", valorNormal: 100, volatilidad: 0, fuerzaDeRetorno: 0.1, valorInicial: 110);
        var azar = new Random(1);

        factor.Avanzar(azar);
        Assert.Equal(109.0, factor.ValorActual, precision: 10);

        factor.Avanzar(azar);
        Assert.Equal(108.1, factor.ValorActual, precision: 10);
    }

    [Fact]
    public void Avanzar_ConFuerzaUnoYSinVolatilidad_VuelveDeGolpeAlValorNormal()
    {
        var factor = new Factor("Prueba", valorNormal: 100, volatilidad: 0, fuerzaDeRetorno: 1, valorInicial: 150);

        factor.Avanzar(new Random(1));

        Assert.Equal(100.0, factor.ValorActual, precision: 10);
    }

    [Fact]
    public void Avanzar_ConLaMismaSemilla_DaLosMismosValores()
    {
        var factorA = new Factor("A", valorNormal: 2, volatilidad: 0.5, fuerzaDeRetorno: 0.05);
        var factorB = new Factor("B", valorNormal: 2, volatilidad: 0.5, fuerzaDeRetorno: 0.05);
        var azarA = new Random(42);
        var azarB = new Random(42);

        for (var i = 0; i < 100; i++)
        {
            factorA.Avanzar(azarA);
            factorB.Avanzar(azarB);
        }

        Assert.Equal(factorA.ValorActual, factorB.ValorActual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_SinNombre_LanzaExcepcion(string nombreInvalido)
    {
        Assert.Throws<ArgumentException>(() =>
            new Factor(nombreInvalido, valorNormal: 1, volatilidad: 1, fuerzaDeRetorno: 0.1));
    }

    [Fact]
    public void Constructor_ConVolatilidadNegativa_LanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Factor("Prueba", valorNormal: 1, volatilidad: -1, fuerzaDeRetorno: 0.1));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_ConFuerzaDeRetornoFueraDeRango_LanzaExcepcion(double fuerzaInvalida)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Factor("Prueba", valorNormal: 1, volatilidad: 1, fuerzaDeRetorno: fuerzaInvalida));
    }
}