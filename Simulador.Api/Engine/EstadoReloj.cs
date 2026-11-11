namespace Simulador.Api.Engine

{

    // Foto del estado del reloj en un momento dado.

    public record EstadoDelReloj(long Tick, double Velocidad, bool EstaPausado);

}
