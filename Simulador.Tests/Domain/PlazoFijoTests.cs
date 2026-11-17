using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class PlazoFijoTests
{
    [Fact]
    public void Constituir_DescuentaElMontoDelEfectivo()
    {
        var billetera = new Billetera(1_000_000m);

        billetera.ConstituirPlazoFijo(100_000m, dias: 30, tasaNominalAnual: 0.20m, tick: 0);

        Assert.Equal(900_000m, billetera.Efectivo);
        Assert.Equal(100_000m, billetera.ValorDePlazosFijos(0));
    }

    [Fact]
    public void Constituir_ConUnPlazoQueNoExiste_LanzaExcepcion()
    {
        var billetera = new Billetera(1_000_000m);

        Assert.Throws<OperacionInvalidaException>(() => billetera.ConstituirPlazoFijo(100_000m, dias: 45, tasaNominalAnual: 0.20m, tick: 0));
    }

    [Fact]
    public void Constituir_ConMasQueElEfectivo_LanzaExcepcionYNoCambiaNada()
    {
        var billetera = new Billetera(1_000m);

        Assert.Throws<OperacionInvalidaException>(() => billetera.ConstituirPlazoFijo(2_000m, dias: 30, tasaNominalAnual: 0.20m, tick: 0));
        Assert.Equal(1_000m, billetera.Efectivo);
        Assert.Empty(billetera.PlazosFijos);
    }

    [Fact]
    public void Cobrar_AntesDelVencimiento_LanzaExcepcion()
    {
        var billetera = new Billetera(1_000_000m);
        var plazoFijo = billetera.ConstituirPlazoFijo(100_000m, dias: 30, tasaNominalAnual: 0.20m, tick: 0);

        Assert.Throws<OperacionInvalidaException>(() => billetera.CobrarPlazoFijo(plazoFijo.Id, tick: 29));
    }

    [Fact]
    public void Cobrar_AlVencer_DevuelveCapitalMasInteresSimple()
    {
        var billetera = new Billetera(1_000_000m);
        var plazoFijo = billetera.ConstituirPlazoFijo(100_000m, dias: 30, tasaNominalAnual: 0.20m, tick: 0);

        billetera.CobrarPlazoFijo(plazoFijo.Id, tick: 30);

        // 100.000 × 20 % × 30 / 365 = 1.643,84
        Assert.Equal(1_001_643.84m, billetera.Efectivo);
        Assert.Equal(0m, billetera.ValorDePlazosFijos(30));
    }

    [Fact]
    public void InteresAcumulado_CreceConLosDiasYNoPasaDelVencimiento()
    {
        var plazoFijo = new PlazoFijo(1, 100_000m, 0.20m, dias: 30, tickDeInicio: 10);

        Assert.Equal(0m, plazoFijo.InteresAcumulado(10));
        Assert.Equal(821.92m, plazoFijo.InteresAcumulado(25));   // 15 días
        Assert.Equal(1_643.84m, plazoFijo.InteresAcumulado(40));
        Assert.Equal(1_643.84m, plazoFijo.InteresAcumulado(100));
    }

    [Fact]
    public void ProximoVencimiento_EsElMasCercanoSinCobrar()
    {
        var billetera = new Billetera(1_000_000m);
        billetera.ConstituirPlazoFijo(100_000m, dias: 90, tasaNominalAnual: 0.21m, tick: 0);
        var corto = billetera.ConstituirPlazoFijo(100_000m, dias: 30, tasaNominalAnual: 0.20m, tick: 0);

        Assert.Equal(30L, billetera.ProximoVencimiento());

        billetera.CobrarPlazoFijo(corto.Id, tick: 30);
        Assert.Equal(90L, billetera.ProximoVencimiento());
    }

    [Fact]
    public void Tasa_EsLaQueEmpataConLaInflacionMasElPremioDelPlazo()
    {
        // Con 26,8 % de inflación anual: a 365 días empata con 26,8 % y suma 2 puntos.
        Assert.Equal(0.2880m, TasasDePlazoFijo.Calcular(365, 0.268));
        // A 30 días la tasa que empata es menor (24 %), porque es interés simple; suma medio punto.
        Assert.Equal(0.2448m, TasasDePlazoFijo.Calcular(30, 0.268));
    }

    [Fact]
    public void Tasa_ConUnPlazoQueNoExiste_LanzaExcepcion()
    {
        Assert.Throws<OperacionInvalidaException>(() => TasasDePlazoFijo.Calcular(45, 0.268));
    }

    [Fact]
    public void InflacionDelUltimoAnio_ConInflacionFija_EsLaAnualCompuesta()
    {
        // 3 % mensual fijo = 0,1 % diario; en un año: 1,001^365 − 1 = 44,03 %.
        var inflacion = new Factor("Inflación", valorNormal: 3, volatilidad: 0, fuerzaDeRetorno: 0.1);
        var mundo = new Mundo(new[] { inflacion }, Array.Empty<Activo>(), new Random(1), "Inflación");

        Assert.Equal(0.4403, mundo.InflacionDelUltimoAnio(), 4);   // día 0: usa la inflación de hoy

        mundo.AvanzarTicks(400);
        Assert.Equal(0.4403, mundo.InflacionDelUltimoAnio(), 4);   // día 400: mira los últimos 365 días
    }
}