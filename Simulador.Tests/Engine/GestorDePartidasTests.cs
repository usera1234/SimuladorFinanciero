using Microsoft.Extensions.Time.Testing;
using Simulador.Api.Engine;

namespace Simulador.Tests.Engine;

public class GestorDePartidasTests
{
    private readonly FakeTimeProvider _tiempo = new();

    [Fact]
    public void Crear_DevuelvePartidasDistintasConMundosDistintos()
    {
        var gestor = new GestorDePartidas(_tiempo);

        var partidaA = gestor.Crear();
        var partidaB = gestor.Crear();

        Assert.NotEqual(partidaA.Id, partidaB.Id);
        Assert.NotSame(partidaA.Reloj.Mundo, partidaB.Reloj.Mundo);
    }

    [Fact]
    public void Crear_LaPartidaEmpiezaEnPausa()
    {
        var gestor = new GestorDePartidas(_tiempo);

        var partida = gestor.Crear();
        _tiempo.Advance(TimeSpan.FromSeconds(30));

        Assert.True(partida.Reloj.EstaPausado);
        Assert.Equal(0L, partida.Reloj.ObtenerEstado().Tick);
    }

    [Fact]
    public void Obtener_DevuelveLaPartidaCreada()
    {
        var gestor = new GestorDePartidas(_tiempo);
        var partida = gestor.Crear();

        Assert.Same(partida, gestor.Obtener(partida.Id));
    }

    [Fact]
    public void Obtener_ConIdInexistente_DevuelveNull()
    {
        var gestor = new GestorDePartidas(_tiempo);

        Assert.Null(gestor.Obtener(Guid.NewGuid()));
    }

    [Fact]
    public void Crear_ConLaMismaSemilla_TieneElMismoMercado()
    {
        var gestor = new GestorDePartidas(_tiempo);
        var partidaA = gestor.Crear(semilla: 42);
        var partidaB = gestor.Crear(semilla: 42);

        partidaA.Reloj.Reanudar();
        partidaB.Reloj.Reanudar();
        _tiempo.Advance(TimeSpan.FromSeconds(200));

        var preciosA = partidaA.Reloj.ObtenerEstado().Activos.Select(activo => activo.Precio);
        var preciosB = partidaB.Reloj.ObtenerEstado().Activos.Select(activo => activo.Precio);
        Assert.Equal(preciosA, preciosB);
    }
}