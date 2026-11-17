using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Una partida de un jugador: su propio mundo con su propio reloj, y su billetera.
public class Partida
{
    // Plata con la que arranca el jugador, en pesos.
    public const decimal CapitalInicial = 1_000_000m;

    // Cantidad de operaciones recientes que se muestran en el estado.
    public const int OperacionesMostradas = 20;

    public Guid Id { get; }

    // Semilla del mercado. Dos partidas con la misma semilla tienen el mismo mercado.
    public int Semilla { get; }

    public RelojDeSimulacion Reloj { get; }

    public Billetera Billetera { get; }

    public Partida(Guid id, int semilla, RelojDeSimulacion reloj)
    {
        Id = id;
        Semilla = semilla;
        Reloj = reloj;
        Billetera = new Billetera(CapitalInicial);
    }

    // Compra al precio del instante actual.
    public Operacion Comprar(string nombreDelActivo, decimal cantidad)
    {
        return Reloj.EjecutarSincronizado(mundo =>
            Billetera.Comprar(BuscarActivo(mundo, nombreDelActivo), cantidad, mundo.TickActual));
    }

    // Vende al precio del instante actual.
    public Operacion Vender(string nombreDelActivo, decimal cantidad)
    {
        return Reloj.EjecutarSincronizado(mundo =>
            Billetera.Vender(BuscarActivo(mundo, nombreDelActivo), cantidad, mundo.TickActual));
    }

    // Devuelve una foto del mundo y de la billetera, tomadas en el mismo instante.
    public EstadoDePartida ObtenerEstado()
    {
        return Reloj.EjecutarSincronizado(mundo =>
            new EstadoDePartida(Reloj.ObtenerEstado(), ArmarEstadoDeBilletera(mundo)));
    }

    // Evolución de la billetera y de los precios desde el inicio, con como máximo "maximoDePuntos" días.
    public HistorialDePartida ObtenerHistorial(int maximoDePuntos)
    {
        return Reloj.EjecutarSincronizado(mundo =>
        {
            var puntos = CalculadoraDeHistorial.ElegirPuntos(mundo.Historial, maximoDePuntos);
            var nombres = mundo.Activos.Select(activo => activo.Nombre).ToList();
            return CalculadoraDeHistorial.Calcular(puntos, nombres, Billetera.CapitalInicial, Billetera.Operaciones);
        });
    }

    private EstadoDeBilletera ArmarEstadoDeBilletera(Mundo mundo)
    {
        var posiciones = mundo.Activos
            .Where(activo => Billetera.CantidadDe(activo.Nombre) > 0)
            .Select(activo =>
            {
                var cantidad = Billetera.CantidadDe(activo.Nombre);
                var precio = (decimal)activo.PrecioActual;
                return new EstadoDePosicion(activo.Nombre, cantidad, precio, Math.Round(cantidad * precio, 2));
            })
            .ToList();

        var valorTotal = Math.Round(Billetera.CalcularValor(mundo.Activos), 2);
        var valorReal = Math.Round(valorTotal / (decimal)mundo.IndiceDePrecios, 2);

        var ultimasOperaciones = Billetera.Operaciones
            .Reverse()
            .Take(OperacionesMostradas)
            .ToList();

        return new EstadoDeBilletera(
            Billetera.CapitalInicial,
            Billetera.Efectivo,
            posiciones,
            valorTotal,
            CalcularGananciaPorcentual(valorTotal),
            valorReal,
            CalcularGananciaPorcentual(valorReal),
            ultimasOperaciones);
    }

    private decimal CalcularGananciaPorcentual(decimal valor)
    {
        return Math.Round((valor / Billetera.CapitalInicial - 1) * 100, 2);
    }

    private static Activo BuscarActivo(Mundo mundo, string nombre)
    {
        return mundo.Activos.FirstOrDefault(activo =>
                   string.Equals(activo.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
               ?? throw new OperacionInvalidaException($"No existe el activo '{nombre}'.");
    }
}