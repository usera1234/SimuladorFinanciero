using Simulador.Api.Domain;

namespace Simulador.Api.Engine;


/// Convierte el tiempo real en ticks de simulación, según la velocidad.
/// El mundo se pone al día cuando se llama a Sincronizar.

public class RelojDeSimulacion
{
   
    /// Ticks que avanzan por segundo real cuando la velocidad es 1.
 
    public const double TicksPorSegundoBase = 1.0;

    private readonly TimeProvider _tiempo;
    private DateTimeOffset _ultimaSincronizacion;
    private double _restoDeTick;

    public Mundo Mundo { get; }
    public double Velocidad { get; private set; }
    public bool EstaPausado { get; private set; }

    public RelojDeSimulacion(Mundo mundo, TimeProvider tiempo, double velocidadInicial = 1.0)
    {
        if (velocidadInicial <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(velocidadInicial),
                "La velocidad debe ser mayor que cero.");
        }

        Mundo = mundo;
        _tiempo = tiempo;
        Velocidad = velocidadInicial;
        _ultimaSincronizacion = _tiempo.GetUtcNow();
    }

    /// Pone al día el mundo según el tiempo real transcurrido desde la última sincronización.
    
    public void Sincronizar()
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
}