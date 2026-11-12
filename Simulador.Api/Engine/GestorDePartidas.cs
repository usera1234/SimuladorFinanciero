using System.Collections.Concurrent;

namespace Simulador.Api.Engine;

// Crea y guarda las partidas en curso.
public class GestorDePartidas
{
    private readonly ConcurrentDictionary<Guid, Partida> _partidas = new();
    private readonly TimeProvider _tiempo;

    public GestorDePartidas(TimeProvider tiempo)
    {
        _tiempo = tiempo;
    }

    // Crea una partida nueva, en pausa. Si no se indica semilla, se elige una al azar.
    public Partida Crear(int? semilla = null)
    {
        var semillaElegida = semilla ?? Random.Shared.Next();
        var mundo = ConfiguracionDelMercado.CrearMundo(semillaElegida);
        var reloj = new RelojDeSimulacion(mundo, _tiempo, iniciarPausado: true);
        var partida = new Partida(Guid.NewGuid(), semillaElegida, reloj);

        _partidas[partida.Id] = partida;
        return partida;
    }

    // Devuelve la partida con ese identificador, o null si no existe.
    public Partida? Obtener(Guid id)
    {
        return _partidas.TryGetValue(id, out var partida) ? partida : null;
    }
}