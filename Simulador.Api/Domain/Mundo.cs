namespace Simulador.Api.Domain

/// <summary>
/// Representa el estado del mundo simulado.
/// Sabe en qué tick está y cómo avanzar.
/// </summary>

{
    public class Mundo
    {

        /// Tick actual de la simulación. Empieza en 0.
        public long TickActual { get; private set; }

        /// Avanza la simulación la cantidad de ticks indicada.

        /// <param name="cantidad">Cantidad de ticks a avanzar. No puede ser negativa.</param>

        public void AvanzarTicks(long cantidad)
        {
            if (cantidad < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cantidad),
                    "La cantidad de ticks no puede ser negativa.");
            }

            TickActual += cantidad;
        }

    }
}
