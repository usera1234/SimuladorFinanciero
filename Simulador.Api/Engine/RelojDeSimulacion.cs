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

    public Mundo Mundo { get; }

    public double Velocidad { get; private set; }

    public bool EstaPausado { get; private set; }

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

            return new EstadoDelMundo(Mundo.TickActual, Velocidad, EstaPausado, factores, activos, ultimosEventos);

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

    private void SincronizarSinBloquear()

    {

        var ahora = _tiempo.GetUtcNow();
        var segundosTranscurridos = (ahora - _ultimaSincronizacion).TotalSeconds;
        _ultimaSincronizacion = ahora;

        if (EstaPausado)

        {

            return;

        }

        var ticksExactos = segundosTranscurridos * Velocidad * TicksPorSegundoBase + _restoDeTick;
        var ticksEnteros = (long)Math.Floor(ticksExactos);
        _restoDeTick = ticksExactos - ticksEnteros;

        Mundo.AvanzarTicks(ticksEnteros);

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