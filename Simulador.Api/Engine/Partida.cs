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

        // Un evento baja la velocidad solo si es de una empresa de la que el jugador tiene acciones,
        // y nunca a la velocidad máxima: si el jugador eligió ir lo más rápido posible, se respeta.
        Reloj.DebeFrenarAnte = evento =>
            Reloj.Velocidad < RelojDeSimulacion.VelocidadMaxima
            && Billetera.CantidadDe(evento.Empresa) > 0;
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

    // Pasa efectivo a un plazo fijo y avisa al reloj cuándo tiene que pausarse.
    public PlazoFijo ConstituirPlazoFijo(decimal monto, int dias)
    {
        return Reloj.EjecutarSincronizado(mundo =>
        {
            var tasa = TasasDePlazoFijo.Calcular(dias, mundo.InflacionDelUltimoAnio());
            var plazoFijo = Billetera.ConstituirPlazoFijo(monto, dias, tasa, mundo.TickActual);
            ActualizarParada();
            return plazoFijo;
        });
    }

    // Cobra un plazo fijo vencido. Si "renovar" es true, vuelve a depositar capital más interés al mismo plazo,
    // con la tasa de hoy (que puede ser distinta de la que tenía).
    public void ResolverPlazoFijo(int id, bool renovar)
    {
        Reloj.EjecutarSincronizado(mundo =>
        {
            var cobrado = Billetera.CobrarPlazoFijo(id, mundo.TickActual);
            if (renovar)
            {
                var tasa = TasasDePlazoFijo.Calcular(cobrado.Dias, mundo.InflacionDelUltimoAnio());
                Billetera.ConstituirPlazoFijo(cobrado.Capital + cobrado.InteresTotal, cobrado.Dias, tasa, mundo.TickActual);
            }

            ActualizarParada();
            return cobrado;
        });
    }

    // Reanuda el tiempo, salvo que haya un plazo fijo vencido esperando una decisión.
    public void Reanudar()
    {
        Reloj.EjecutarSincronizado(mundo =>
        {
            if (Billetera.HayPlazoFijoVencido(mundo.TickActual))
            {
                throw new OperacionInvalidaException(
                    "Tenés un plazo fijo vencido: elegí si lo renovás o pasás la plata al efectivo.");
            }

            // El candado de C# admite que el mismo hilo lo vuelva a tomar, así que esto no se traba.
            Reloj.Reanudar();
            return true;
        });
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
            return CalculadoraDeHistorial.Calcular(
                puntos, nombres, Billetera.CapitalInicial, Billetera.Operaciones, Billetera.PlazosFijos);
        });
    }

    // El reloj se pausa solo el día del próximo vencimiento. Se llama siempre dentro del candado.
    private void ActualizarParada()
    {
        Reloj.TickDeParada = Billetera.ProximoVencimiento();
    }

    private EstadoDeBilletera ArmarEstadoDeBilletera(Mundo mundo)
    {
        var tick = mundo.TickActual;

        var posiciones = mundo.Activos
            .Where(activo => Billetera.CantidadDe(activo.Nombre) > 0)
            .Select(activo =>
            {
                var cantidad = Billetera.CantidadDe(activo.Nombre);
                var precio = (decimal)activo.PrecioActual;
                return new EstadoDePosicion(activo.Nombre, cantidad, precio, Math.Round(cantidad * precio, 2));
            })
            .ToList();

        var plazosFijos = Billetera.PlazosFijos
            .Where(p => p.TickDeCobro is null)
            .Select(p => new EstadoDePlazoFijo(
                p.Id, p.Capital, p.TasaNominalAnual, p.Dias, p.TickDeInicio, p.TickDeVencimiento,
                p.InteresAcumulado(tick), p.Capital + p.InteresTotal, p.EstaVencido(tick)))
            .ToList();

        // Las tasas que se ofrecen hoy, calculadas con la inflación del último año.
        var inflacionDelUltimoAnio = mundo.InflacionDelUltimoAnio();
        var opciones = TasasDePlazoFijo.PlazosDisponibles
            .Select(dias => new OpcionDePlazoFijo(dias, TasasDePlazoFijo.Calcular(dias, inflacionDelUltimoAnio)))
            .ToList();

        var valorTotal = Math.Round(Billetera.CalcularValor(mundo.Activos) + Billetera.ValorDePlazosFijos(tick), 2);
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
            ultimasOperaciones,
            plazosFijos,
            opciones,
            Math.Round(inflacionDelUltimoAnio * 100, 2));
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