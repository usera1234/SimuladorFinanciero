namespace Simulador.Api.Domain;

// Dinero del jugador: efectivo, cantidad de cada activo, plazos fijos y registro de operaciones.
// Usa decimal para que las cuentas con dinero sean exactas.
public class Billetera
{
    // Comisión por compra o venta (0,5 % del importe). Los plazos fijos no pagan comisión.
    public const decimal TasaDeComision = 0.005m;

    private readonly Dictionary<string, decimal> _posiciones = new();
    private readonly List<Operacion> _operaciones = new();
    private readonly List<PlazoFijo> _plazosFijos = new();

    public decimal CapitalInicial { get; }
    public decimal Efectivo { get; private set; }

    // Cantidad de cada activo, por nombre. Solo incluye cantidades mayores que cero.
    public IReadOnlyDictionary<string, decimal> Posiciones => _posiciones;

    // Todas las operaciones, de la más vieja a la más nueva.
    public IReadOnlyList<Operacion> Operaciones => _operaciones;

    // Todos los plazos fijos, incluidos los ya cobrados (hacen falta para reconstruir el historial).
    public IReadOnlyList<PlazoFijo> PlazosFijos => _plazosFijos;

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

    // Pasa efectivo a un plazo fijo con la tasa indicada (la calcula la partida según la inflación).
    public PlazoFijo ConstituirPlazoFijo(decimal monto, int dias, decimal tasaNominalAnual, long tick)
    {
        if (monto <= 0)
        {
            throw new OperacionInvalidaException("El monto del plazo fijo debe ser mayor que cero.");
        }

        if (!TasasDePlazoFijo.PremioPorDias.ContainsKey(dias))
        {
            var opciones = string.Join(", ", TasasDePlazoFijo.PlazosDisponibles);
            throw new OperacionInvalidaException($"El plazo debe ser de {opciones} días.");
        }

        if (monto > Efectivo)
        {
            throw new OperacionInvalidaException(
                $"Efectivo insuficiente: querés depositar ${monto:N2} y hay ${Efectivo:N2}.");
        }

        var plazoFijo = new PlazoFijo(_plazosFijos.Count + 1, monto, tasaNominalAnual, dias, tick);
        _plazosFijos.Add(plazoFijo);
        Efectivo -= monto;

        Registrar(new Operacion(tick, TipoDeOperacion.PlazoFijo, NombreDe(plazoFijo), monto, 1m, 0m, monto));
        return plazoFijo;
    }

    // Cobra un plazo fijo vencido: capital más interés vuelven al efectivo. Antes del vencimiento no se puede.
    public PlazoFijo CobrarPlazoFijo(int id, long tick)
    {
        var plazoFijo = _plazosFijos.FirstOrDefault(p => p.Id == id)
            ?? throw new OperacionInvalidaException($"No existe el plazo fijo {id}.");

        if (plazoFijo.TickDeCobro is not null)
        {
            throw new OperacionInvalidaException("Ese plazo fijo ya fue cobrado.");
        }

        if (!plazoFijo.EstaVencido(tick))
        {
            throw new OperacionInvalidaException(
                $"El plazo fijo vence el día {plazoFijo.TickDeVencimiento}: no se puede retirar antes.");
        }

        var total = plazoFijo.Capital + plazoFijo.InteresTotal;
        Efectivo += total;
        plazoFijo.MarcarCobrado(tick);

        Registrar(new Operacion(tick, TipoDeOperacion.CobroDePlazoFijo, NombreDe(plazoFijo), plazoFijo.Capital, 1m, 0m, total));
        return plazoFijo;
    }

    // Valor de los plazos fijos activos ese día: capital más interés ganado hasta ahí.
    public decimal ValorDePlazosFijos(long tick)
    {
        return _plazosFijos.Sum(p => p.ValorEn(tick));
    }

    // Si hay algún plazo fijo vencido esperando que el jugador decida.
    public bool HayPlazoFijoVencido(long tick)
    {
        return _plazosFijos.Any(p => p.EstaVencido(tick));
    }

    // Día del próximo vencimiento entre los plazos fijos sin cobrar, o null si no hay ninguno.
    public long? ProximoVencimiento()
    {
        var pendientes = _plazosFijos.Where(p => p.TickDeCobro is null).ToList();
        return pendientes.Count == 0 ? null : pendientes.Min(p => p.TickDeVencimiento);
    }

    // Cantidad que tiene el jugador de un activo (0 si no tiene).
    public decimal CantidadDe(string nombreDelActivo)
    {
        return _posiciones.TryGetValue(nombreDelActivo, out var cantidad) ? cantidad : 0;
    }

    // Efectivo más el valor actual de cada posición. No incluye los plazos fijos (ver ValorDePlazosFijos).
    public decimal CalcularValor(IEnumerable<Activo> activos)
    {
        var valor = Efectivo;

        foreach (var activo in activos)
        {
            valor += CantidadDe(activo.Nombre) * (decimal)activo.PrecioActual;
        }

        return valor;
    }

    private static string NombreDe(PlazoFijo plazoFijo)
    {
        return $"Plazo fijo a {plazoFijo.Dias} días";
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