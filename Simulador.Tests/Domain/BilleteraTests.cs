using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class BilleteraTests
{
    private const decimal CapitalInicial = 1_000_000m;

    // Activos con precio fijo: sin base, sin volatilidad y sin avanzar, el precio no cambia.
    private static readonly Activo Dolar =
        new("Dólar", TipoDeActivo.Moneda, 1_000, 0, 0, new Dictionary<string, double>());

    private static readonly Activo Accion =
        new("Acción", TipoDeActivo.Accion, 500, 0, 0, new Dictionary<string, double>());

    private static readonly Activo Cripto =
        new("Cripto", TipoDeActivo.Cripto, 100_000, 0, 0, new Dictionary<string, double>());

    [Fact] 
    public void NuevaBilletera_TieneTodoEnEfectivo()
    {
        var billetera = new Billetera(CapitalInicial);

        Assert.Equal(CapitalInicial, billetera.Efectivo);
        Assert.Empty(billetera.Posiciones);
    }

    [Fact]
    public void Comprar_DescuentaElImporteYLaComision()
    {
        var billetera = new Billetera(CapitalInicial);

        // 10 × $1.000 = $10.000, más 0,5% de comisión = $50.
        var operacion = billetera.Comprar(Dolar, 10, tick: 1);

        Assert.Equal(989_950m, billetera.Efectivo);
        Assert.Equal(10m, billetera.CantidadDe("Dólar"));
        Assert.Equal(50m, operacion.Comision);
        Assert.Equal(10_050m, operacion.Total);
    }

    [Fact]
    public void Vender_AcreditaElImporteMenosLaComision()
    {
        var billetera = new Billetera(CapitalInicial);
        billetera.Comprar(Dolar, 10, tick: 1);

        // 4 × $1.000 = $4.000, menos $20 de comisión = $3.980.
        billetera.Vender(Dolar, 4, tick: 2);

        Assert.Equal(993_930m, billetera.Efectivo);
        Assert.Equal(6m, billetera.CantidadDe("Dólar"));
    }

    [Fact]
    public void Vender_TodoLoQueTiene_EliminaLaPosicion()
    {
        var billetera = new Billetera(CapitalInicial);
        billetera.Comprar(Dolar, 10, tick: 1);

        billetera.Vender(Dolar, 10, tick: 2);

        Assert.Empty(billetera.Posiciones);
    }

    [Fact]
    public void ComprarYVenderEnseguida_PierdeDosComisiones()
    {
        var billetera = new Billetera(CapitalInicial);

        billetera.Comprar(Dolar, 10, tick: 1);
        billetera.Vender(Dolar, 10, tick: 1);

        Assert.Equal(CapitalInicial - 100m, billetera.Efectivo);
    }

    [Fact]
    public void Comprar_SinEfectivoSuficiente_LanzaExcepcionYNoCambiaNada()
    {
        var billetera = new Billetera(CapitalInicial);

        // 1.000 × $1.000 = $1.000.000, más $5.000 de comisión: no alcanza.
        Assert.Throws<OperacionInvalidaException>(() => billetera.Comprar(Dolar, 1_000, tick: 1));
        Assert.Equal(CapitalInicial, billetera.Efectivo);
        Assert.Empty(billetera.Operaciones);
    }

    [Fact]
    public void Vender_MasDeLoQueTiene_LanzaExcepcion()
    {
        var billetera = new Billetera(CapitalInicial);
        billetera.Comprar(Dolar, 10, tick: 1);

        Assert.Throws<OperacionInvalidaException>(() => billetera.Vender(Dolar, 11, tick: 2));
    }

    [Fact]
    public void Comprar_AccionConFraccion_LanzaExcepcion()
    {
        var billetera = new Billetera(CapitalInicial);

        Assert.Throws<OperacionInvalidaException>(() => billetera.Comprar(Accion, 2.5m, tick: 1));
    }

    [Fact]
    public void Comprar_CriptoConFraccion_Funciona()
    {
        var billetera = new Billetera(CapitalInicial);

        billetera.Comprar(Cripto, 0.5m, tick: 1);

        Assert.Equal(0.5m, billetera.CantidadDe("Cripto"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Comprar_ConCantidadNoPositiva_LanzaExcepcion(int cantidadInvalida)
    {
        var billetera = new Billetera(CapitalInicial);

        Assert.Throws<OperacionInvalidaException>(() => billetera.Comprar(Dolar, cantidadInvalida, tick: 1));
    }

    [Fact]
    public void CalcularValor_SumaEfectivoYPosiciones()
    {
        var billetera = new Billetera(CapitalInicial);
        billetera.Comprar(Dolar, 10, tick: 1);

        // $989.950 de efectivo + 10 × $1.000 = $999.950 (se perdieron $50 de comisión).
        Assert.Equal(999_950m, billetera.CalcularValor(new[] { Dolar }));
    }

    [Fact]
    public void Operaciones_SeRegistranConSuTickYTipo()
    {
        var billetera = new Billetera(CapitalInicial);

        billetera.Comprar(Dolar, 10, tick: 5);
        billetera.Vender(Dolar, 3, tick: 9);

        Assert.Collection(billetera.Operaciones,
            compra => { Assert.Equal(5L, compra.Tick); Assert.Equal(TipoDeOperacion.Compra, compra.Tipo); },
            venta => { Assert.Equal(9L, venta.Tick); Assert.Equal(TipoDeOperacion.Venta, venta.Tipo); });
    }

    [Fact]
    public void Constructor_ConCapitalNoPositivo_LanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Billetera(0));
    }
}