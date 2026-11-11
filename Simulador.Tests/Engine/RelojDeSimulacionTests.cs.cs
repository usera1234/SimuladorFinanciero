using Microsoft.Extensions.Time.Testing;
using Simulador.Api.Domain;
using Simulador.Api.Engine;

namespace Simulador.Tests.Engine;

public class RelojDeSimulacionTests
{
    private readonly FakeTimeProvider _tiempo = new();

    private RelojDeSimulacion CrearReloj(double velocidad = 1.0)
    {
        return new RelojDeSimulacion(new Mundo(), _tiempo, velocidad);
    }

    private void PasarSegundos(double segundos)
    {
        _tiempo.Advance(TimeSpan.FromSeconds(segundos));
    }

    [Fact]
    public void Sincronizar_SinTiempoTranscurrido_NoAvanza()
    {
        var reloj = CrearReloj();

        reloj.Sincronizar();

        Assert.Equal(0L, reloj.Mundo.TickActual);
    }

    [Fact]
    public void Sincronizar_AvanzaSegunTiempoYVelocidad()
    {
        var reloj = CrearReloj(velocidad: 3);

        PasarSegundos(4);
        reloj.Sincronizar();

        Assert.Equal(12L, reloj.Mundo.TickActual);
    }

    [Fact]
    public void Sincronizar_AcumulaFraccionesDeTick()
    {
        var reloj = CrearReloj();

        PasarSegundos(0.5);
        reloj.Sincronizar();
        Assert.Equal(0L, reloj.Mundo.TickActual);

        PasarSegundos(0.5);
        reloj.Sincronizar();
        Assert.Equal(1L, reloj.Mundo.TickActual);
    }

    [Fact]
    public void Pausado_NoAvanzaAunquePaseElTiempo()
    {
        var reloj = CrearReloj();

        reloj.Pausar();
        PasarSegundos(60);
        reloj.Sincronizar();

        Assert.Equal(0L, reloj.Mundo.TickActual);
        Assert.True(reloj.EstaPausado);
    }

    [Fact]
    public void Reanudar_NoCuentaElTiempoEnPausa()
    {
        var reloj = CrearReloj();

        reloj.Pausar();
        PasarSegundos(60);
        reloj.Reanudar();
        PasarSegundos(2);
        reloj.Sincronizar();

        Assert.Equal(2L, reloj.Mundo.TickActual);
    }

    [Fact]
    public void CambiarVelocidad_ElTiempoAnteriorUsaLaVelocidadVieja()
    {
        var reloj = CrearReloj(velocidad: 1);

        PasarSegundos(10);
        reloj.CambiarVelocidad(10);
        PasarSegundos(1);
        reloj.Sincronizar();

        Assert.Equal(20L, reloj.Mundo.TickActual);
    }

    [Fact]
    public void TablaDelRelojPerezoso_DaLosValoresEsperados()
    {
        var reloj = CrearReloj(velocidad: 2);

        PasarSegundos(3.7);
        Assert.Equal(7L, reloj.ObtenerEstado().Tick);

        PasarSegundos(1.3);
        reloj.Pausar();
        Assert.Equal(10L, reloj.Mundo.TickActual);

        PasarSegundos(55);
        Assert.Equal(10L, reloj.ObtenerEstado().Tick);

        PasarSegundos(1);
        reloj.Reanudar();
        Assert.Equal(10L, reloj.Mundo.TickActual);
        Assert.False(reloj.EstaPausado);
        Assert.Equal(2.0, reloj.Velocidad);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CambiarVelocidad_ConValorInvalido_LanzaExcepcion(double velocidadInvalida)
    {
        var reloj = CrearReloj();

        Assert.Throws<ArgumentOutOfRangeException>(() => reloj.CambiarVelocidad(velocidadInvalida));
    }

    [Fact]
    public void ObtenerEstado_IncluyeLosFactoresPuestosAlDia()
    {
        var factor = new Factor("Prueba", valorNormal: 100, volatilidad: 0, fuerzaDeRetorno: 0.1, valorInicial: 110);
        var mundo = new Mundo(new[] { factor }, new Random(1));
        var reloj = new RelojDeSimulacion(mundo, _tiempo);

        PasarSegundos(2);
        var estado = reloj.ObtenerEstado();

        var estadoDelFactor = Assert.Single(estado.Factores);
        Assert.Equal("Prueba", estadoDelFactor.Nombre);
        Assert.Equal(108.1, estadoDelFactor.Valor, precision: 10);
    }
}