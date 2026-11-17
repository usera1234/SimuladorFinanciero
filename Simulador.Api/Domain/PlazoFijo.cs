namespace Simulador.Api.Domain;

// Un depósito a plazo fijo: la plata queda inmovilizada hasta el vencimiento y gana interés simple.
// No se puede retirar antes de vencer. Al vencer, el jugador decide si lo renueva o lo cobra.
public class PlazoFijo
{
    public int Id { get; }
    public decimal Capital { get; }

    // Tasa nominal anual como proporción (0,20 = 20 % anual). Queda fija desde que se constituye.
    public decimal TasaNominalAnual { get; }

    public int Dias { get; }
    public long TickDeInicio { get; }
    public long TickDeVencimiento => TickDeInicio + Dias;

    // Día en que el jugador lo cobró (o lo renovó). null mientras no lo haya hecho.
    public long? TickDeCobro { get; private set; }

    public PlazoFijo(int id, decimal capital, decimal tasaNominalAnual, int dias, long tickDeInicio)
    {
        Id = id;
        Capital = capital;
        TasaNominalAnual = tasaNominalAnual;
        Dias = dias;
        TickDeInicio = tickDeInicio;
    }

    // Interés total que paga al vencer: capital × tasa anual × días / 365 (interés simple, como en los bancos).
    public decimal InteresTotal => CalcularInteres(Dias);

    // Interés ganado hasta un día dado. No sigue creciendo después del vencimiento.
    public decimal InteresAcumulado(long tick)
    {
        var diasTranscurridos = Math.Clamp(tick - TickDeInicio, 0, Dias);
        return CalcularInteres(diasTranscurridos);
    }

    // Si ese día el depósito existía y todavía no se había cobrado.
    public bool EstaActivo(long tick)
    {
        return tick >= TickDeInicio && (TickDeCobro is null || tick < TickDeCobro);
    }

    // Si ya venció y el jugador todavía no decidió qué hacer.
    public bool EstaVencido(long tick)
    {
        return TickDeCobro is null && tick >= TickDeVencimiento;
    }

    // Cuánto vale el depósito ese día: capital más el interés ganado hasta ahí. Cero si no estaba activo.
    public decimal ValorEn(long tick)
    {
        return EstaActivo(tick) ? Capital + InteresAcumulado(tick) : 0;
    }

    // Lo llama la billetera al cobrarlo.
    public void MarcarCobrado(long tick)
    {
        TickDeCobro = tick;
    }

    private decimal CalcularInteres(long dias)
    {
        return Math.Round(Capital * TasaNominalAnual * dias / 365m, 2);
    }
}