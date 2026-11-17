using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Convierte el tiempo real en ticks de simulación, según la velocidad.
// Funciona como un reloj perezoso: el mundo se pone al día cuando alguien lo consulta o lo modifica.

public class RelojDeSimulacion
{

    // Ticks que avanzan por segundo real cuando la velocidad es 1.

    public const double TicksPorSegundoBase = 1.0;

    // Velocidad máxima permitida. 

    public const double VelocidadMaxima = 100.0;

    private readonly TimeProvider _tiempo;

    private readonly object _candado = new(); 

    private DateTimeOffset _ultimaSincronizacion;

    private double _restoDeTick;

    // Velocidad a la que baja el reloj cuando ocurre un evento que le importa al jugador.
    public const double VelocidadAnteEvento = 1.0;

    public Mundo Mundo { get; }

    public double Velocidad { get; private set; }

    public bool EstaPausado { get; private set; }

    // Si tiene valor, el reloj se pausa solo al llegar a ese tick (por ejemplo, cuando vence un plazo fijo).
    // Se modifica desde EjecutarSincronizado, dentro del candado.
    public long? TickDeParada { get; set; }

    // Decide si un evento debe bajar la velocidad. Si es null, ningún evento la baja.
    public Func<RegistroDeEvento, bool>? DebeFrenarAnte { get; set; }

    public RelojDeSimulacion(Mundo mundo, TimeProvider tiempo, double velocidadInicial = 1.0, bool iniciarPausado = false)
    {
        ValidarVelocidad(velocidadInicial);

        Mundo = mundo;
        _tiempo = tiempo;
        Velocidad = velocidadInicial;
        EstaPausado = iniciarPausado;
        _ultimaSincronizacion = _tiempo.GetUtcNow();
    }

    // Pone al día el mundo y devuelve una foto de su estado.

    // Pone al día el mundo y devuelve una foto de su estado.
    public EstadoDelMundo ObtenerEstado()

    {

        lock (_candado)

        {

            SincronizarSinBloquear();

            var factores = Mundo.Factores
                .Select(factor => new EstadoDeFactor(factor.Nombre, factor.ValorActual))
                .ToList();

            var activos = Mundo.Activos
                .Select(activo => new EstadoDeActivo(
                    activo.Nombre,
                    activo.Tipo,
                    activo.PrecioActual,
                    (activo.PrecioActual / activo.PrecioInicial - 1) * 100))
                .ToList();

            // Del más nuevo al más viejo, que es como se leen las noticias.
            var ultimosEventos = Mundo.UltimosEventos
                .Reverse()
                .ToList();

            var inflacionAcumulada = (Mundo.IndiceDePrecios - 1) * 100;

            return new EstadoDelMundo(Mundo.TickActual, Velocidad, EstaPausado, inflacionAcumulada,
                factores, activos, ultimosEventos);

        }

    }

    // Pone al día el mundo según el tiempo real transcurrido.

    public void Sincronizar()

    {
        lock (_candado) 

        {

            SincronizarSinBloquear();

        }

    }

    // Pone al día el mundo y ejecuta una acción sobre él, todo dentro del candado.
    // Sirve para operar al precio actual sin que el tiempo avance a la mitad.
    public T EjecutarSincronizado<T>(Func<Mundo, T> accion)

    {

        lock (_candado)
        {

            SincronizarSinBloquear();
            return accion(Mundo);

        }

    }

    // Detiene el avance del tiempo. Si ya estaba pausado, no hace nada. // NUEVO

    public void Pausar()

    {
        lock (_candado)

        {

            SincronizarSinBloquear();
            EstaPausado = true;

        }

    }

    // Reanuda el avance del tiempo con la velocidad que tenía. // NUEVO

    public void Reanudar()

    {

        lock (_candado)

        {

            SincronizarSinBloquear();
            EstaPausado = false;

        }

    }

    // Cambia la velocidad. El tiempo anterior se cuenta con la velocidad vieja. // NUEVO

    public void CambiarVelocidad(double nuevaVelocidad)

    {

        ValidarVelocidad(nuevaVelocidad);

        lock (_candado)

        {

            SincronizarSinBloquear();
            Velocidad = nuevaVelocidad;

        }
    }

    // La lógica de Sincronizar se movió acá, sin el candado

    // Pone al día el mundo según el tiempo real transcurrido. Solo se llama con el candado tomado.
    private void SincronizarSinBloquear()
    {
        var ahora = _tiempo.GetUtcNow();
        var segundosTranscurridos = (ahora - _ultimaSincronizacion).TotalSeconds;
        _ultimaSincronizacion = ahora;

        if (EstaPausado)
        {
            return;
        }

        // Ticks que corresponden al tiempo transcurrido, más la fracción que sobró la vez anterior.
        var ticksDisponibles = segundosTranscurridos * Velocidad * TicksPorSegundoBase + _restoDeTick;

        // Se avanza de a un tick para poder pausar o frenar justo el día en que pasa algo.
        while (ticksDisponibles >= 1)
        {
            if (LlegoALaParada())
            {
                EstaPausado = true;
                _restoDeTick = 0;
                return;
            }

            Mundo.AvanzarTicks(1);
            ticksDisponibles -= 1;

            if (Velocidad > VelocidadAnteEvento && HuboEventoQueFrena())
            {
                // El tiempo real que quedaba se sigue contando, pero a la velocidad nueva.
                ticksDisponibles = ticksDisponibles / Velocidad * VelocidadAnteEvento;
                Velocidad = VelocidadAnteEvento;
            }
        }

        _restoDeTick = ticksDisponibles;

        if (LlegoALaParada())
        {
            EstaPausado = true;
            _restoDeTick = 0;
        }
    }

    private bool LlegoALaParada()
    {
        return TickDeParada is long parada && Mundo.TickActual >= parada;
    }

    // Revisa los eventos del tick que acaba de pasar. Están del más viejo al más nuevo,
    // así que alcanza con mirar el final de la lista.
    private bool HuboEventoQueFrena()
    {
        if (DebeFrenarAnte is null)
        {
            return false;
        }

        var eventos = Mundo.UltimosEventos;
        for (int i = eventos.Count - 1; i >= 0 && eventos[i].Tick == Mundo.TickActual; i--)
        {
            if (DebeFrenarAnte(eventos[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidarVelocidad(double velocidad)

    {

        if (velocidad <= 0 || velocidad > VelocidadMaxima)

        {

            throw new ArgumentOutOfRangeException(
                nameof(velocidad),
                $"La velocidad debe ser mayor que 0 y menor o igual que {VelocidadMaxima}.");

        }

    }

}