namespace Simulador.Api.Engine;

// Una partida de un jugador: su propio mundo con su propio reloj.
public class Partida
{
    public Guid Id { get; }

    // Semilla del mercado. Dos partidas con la misma semilla tienen el mismo mercado.
    public int Semilla { get; }

    public RelojDeSimulacion Reloj { get; }

    public Partida(Guid id, int semilla, RelojDeSimulacion reloj)
    {
        Id = id;
        Semilla = semilla;
        Reloj = reloj;
    }
}