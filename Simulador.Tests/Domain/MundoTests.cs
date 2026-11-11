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
}