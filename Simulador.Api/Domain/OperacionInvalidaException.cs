namespace Simulador.Api.Domain;

// Error causado por una operación que el jugador no puede hacer
// (por ejemplo, comprar sin efectivo suficiente). Su mensaje está pensado para mostrarse al jugador.
public class OperacionInvalidaException : Exception
{
    public OperacionInvalidaException(string mensaje) : base(mensaje)
    {
    }
}