namespace Simulador.Api.Domain;

// Dinero del jugador: efectivo, cantidades de cada activo y registro de operaciones.
// Usa decimal en lugar de double para evitar errores de redondeo con dinero.
public class Billetera
{
    // Comisión por operación: 0,5% del importe, como cobra aproximadamente un broker.
    public const decimal TasaDeComision = 0.005m;

    private readonly Dictionary<string, decimal> _posiciones = new();
    private readonly List<Operacion> _operaciones = new();

    public decimal CapitalInicial { get; }
    public decimal Efectivo { get; private set; }

    // Cantidad de cada activo, por nombre. Solo incluye los activos con cantidad mayor que cero.
    public IReadOnlyDictionary<string, decimal> Posiciones => _posiciones;

    public IReadOnlyList<Operacion> Operaciones => _operaciones;

    public Billetera(decimal capitalInicial)
    {
        if (capitalInicial <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capitalInicial), "El capital inicial debe ser mayor que cero.");
        }

        CapitalInicial = capitalInicial;
        Efectivo = capitalInicial;
    }

    // Compra al precio actual. Descuenta del efectivo el importe más la comisión.
    public Operacion Comprar(Activo activo, decimal cantidad, long tick)
    {
        ValidarCantidad(activo, cantidad);

        var precio = (decimal)activo.PrecioActual;
        var importe = Math.Round(precio * cantidad, 2);
        var comision = Math.Round(importe * TasaDeComision, 2);
        var total = importe + comision;

        if (total > Efectivo)
        {
            throw new OperacionInvalidaException(
                $"Efectivo insuficiente: la compra cuesta ${total:N2} con comisión y hay ${Efectivo:N2}.");
        }

        Efectivo -= total;
        _posiciones[activo.Nombre] = CantidadDe(activo.Nombre) + cantidad;

        return Registrar(new Operacion(tick, TipoDeOperacion.Compra, activo.Nombre, cantidad, precio, comision, total));
    }

    // Vende al precio actual. Suma al efectivo el importe menos la comisión.
    public Operacion Vender(Activo activo, decimal cantidad, long tick)
    {
        ValidarCantidad(activo, cantidad);

        var tenencia = CantidadDe(activo.Nombre);
        if (cantidad > tenencia)
        {
            throw new OperacionInvalidaException(
                $"No tenés suficiente {activo.Nombre}: querés vender {cantidad} y tenés {tenencia}.");
        }

        var precio = (decimal)activo.PrecioActual;
        var importe = Math.Round(precio * cantidad, 2);
        var comision = Math.Round(importe * TasaDeComision, 2);
        var total = importe - comision;

        Efectivo += total;

        var restante = tenencia - cantidad;
        if (restante == 0)
        {
            _posiciones.Remove(activo.Nombre);
        }
        else
        {
            _posiciones[activo.Nombre] = restante;
        }

        return Registrar(new Operacion(tick, TipoDeOperacion.Venta, activo.Nombre, cantidad, precio, comision, total));
    }

    // Cantidad que tiene el jugador de un activo (0 si no tiene).
    public decimal CantidadDe(string nombreDelActivo)
    {
        return _posiciones.TryGetValue(nombreDelActivo, out var cantidad) ? cantidad : 0;
    }

    // Valor total de la billetera en pesos: efectivo más el valor actual de cada posición.
    public decimal CalcularValor(IEnumerable<Activo> activos)
    {
        var valor = Efectivo;

        foreach (var activo in activos)
        {
            valor += CantidadDe(activo.Nombre) * (decimal)activo.PrecioActual;
        }

        return valor;
    }

    private Operacion Registrar(Operacion operacion)
    {
        _operaciones.Add(operacion);
        return operacion;
    }

    private static void ValidarCantidad(Activo activo, decimal cantidad)
    {
        if (cantidad <= 0)
        {
            throw new OperacionInvalidaException("La cantidad debe ser mayor que cero.");
        }

        if (!activo.PermiteFracciones && cantidad != decimal.Truncate(cantidad))
        {
            throw new OperacionInvalidaException(
                $"{activo.Nombre} solo se puede operar por unidades enteras.");
        }
    }
}