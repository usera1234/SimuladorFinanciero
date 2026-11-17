using Simulador.Api.Domain;
using Simulador.Api.Engine;

namespace Simulador.Tests.Engine;

public class CalculadoraDeHistorialTests
{
    private static PuntoDelHistorial Punto(long tick, double precio, double indice = 1.0) =>
        new(tick, indice, new[] { precio });

    [Fact]
    public void Calcular_SinOperaciones_ElValorNominalNoCambiaYElRealCaeConLaInflacion()
    {
        var puntos = new[] { Punto(0, 100), Punto(1, 120, indice: 1.25) };

        var historial = CalculadoraDeHistorial.Calcular(puntos, new[] { "X" }, 10_000m, Array.Empty<Operacion>());

        Assert.Equal(new[] { 10_000m, 10_000m }, historial.ValorTotal);
        Assert.Equal(8_000m, historial.ValorReal[1]);   // 10.000 / 1,25
    }

    [Fact]
    public void Calcular_ConUnaCompra_AplicaLaOperacionDesdeSuDia()
    {
        var puntos = new[] { Punto(0, 100), Punto(1, 100), Punto(2, 110) };
        // 10 unidades a $100 = $1.000, más $5 de comisión.
        var compra = new Operacion(1, TipoDeOperacion.Compra, "X", 10m, 100m, 5m, 1_005m);

        var historial = CalculadoraDeHistorial.Calcular(puntos, new[] { "X" }, 10_000m, new[] { compra });

        Assert.Equal(new[] { 10_000m, 9_995m, 10_095m }, historial.ValorTotal);
    }

    [Fact]
    public void ElegirPuntos_ConMasPuntosQueElMaximo_RepartePorIgualYConservaLosExtremos()
    {
        var historial = Enumerable.Range(0, 1000).Select(tick => Punto(tick, 100)).ToList();

        var elegidos = CalculadoraDeHistorial.ElegirPuntos(historial, 5);

        Assert.Equal(new long[] { 0, 249, 499, 749, 999 }, elegidos.Select(p => p.Tick));
    }

    [Fact]
    public void Historial_DelMundo_TieneUnPuntoPorTickDesdeElCero()
    {
        var activo = new Activo("X", TipoDeActivo.Ahorro, 100, 3, 0, new Dictionary<string, double>());
        var mundo = new Mundo(Array.Empty<Factor>(), new[] { activo }, new Random(1));

        mundo.AvanzarTicks(2);

        Assert.Equal(new long[] { 0, 1, 2 }, mundo.Historial.Select(p => p.Tick));
        Assert.Equal(100, mundo.Historial[0].Precios[0]);
        Assert.Equal(100.1, mundo.Historial[1].Precios[0], 6);   // 3 % mensual = 0,1 % diario
    }
}