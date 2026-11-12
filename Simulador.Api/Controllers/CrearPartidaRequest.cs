namespace Simulador.Api.Controllers;

// Datos opcionales para crear una partida.
public class CrearPartidaRequest
{
    // Semilla del mercado. Si se omite, se elige una al azar.
    public int? Semilla { get; set; }
}