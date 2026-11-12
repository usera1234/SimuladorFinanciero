namespace Simulador.Api.Domain;

// Acción de una empresa: se mueve como cualquier activo, pero además recuerda cómo le viene yendo
// (racha) y puede tener eventos que mueven su precio de golpe.
public class Empresa : Activo
{
    // Cuánto pesa el rendimiento de hoy en la racha (0,05 = 5%). Con este valor, la racha
    // "recuerda" aproximadamente el último mes.
    public const double FactorDeMemoria = 0.05;

    // Racha a partir de la cual se considera un movimiento "fuerte" (1% diario promedio).
    public const double RachaDeReferencia = 0.01;

    // Influencia máxima de la racha sobre las probabilidades (0,1 = 10%).
    public const double InfluenciaMaximaDeLaRacha = 0.1;

    // Ticks sin eventos necesarios para que la probabilidad de evento llegue a duplicarse.
    public const long TicksParaDuplicarProbabilidad = 365;

    private readonly List<DefinicionDeEvento> _eventosPosibles;

    // Probabilidad de evento por tick, antes de los ajustes por tiempo y por racha.
    public double ProbabilidadBaseDiaria { get; }

    public IReadOnlyList<DefinicionDeEvento> EventosPosibles => _eventosPosibles;

    // Resumen de cómo le vino yendo últimamente: positiva si viene subiendo, negativa si viene bajando.
    public double Racha { get; private set; }

    public long TicksSinEvento { get; private set; }

    // Evento ocurrido en el último tick, o null si no hubo.
    public EventoOcurrido? EventoDelUltimoTick { get; private set; }

    public Empresa(
        string nombre,
        double precioInicial,
        double rendimientoBaseMensual,
        double volatilidadDiaria,
        IDictionary<string, double> sensibilidades,
        double probabilidadBaseDiaria,
        IEnumerable<DefinicionDeEvento> eventosPosibles)
        : base(nombre, TipoDeActivo.Accion, precioInicial, rendimientoBaseMensual, volatilidadDiaria, sensibilidades)
    {
        if (probabilidadBaseDiaria < 0 || probabilidadBaseDiaria > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probabilidadBaseDiaria), "La probabilidad debe estar entre 0 y 1.");
        }

        _eventosPosibles = eventosPosibles.ToList();

        if (probabilidadBaseDiaria > 0 && _eventosPosibles.Count == 0)
        {
            throw new ArgumentException("Una empresa con probabilidad de eventos necesita al menos un evento posible.");
        }

        foreach (var evento in _eventosPosibles)
        {
            if (evento.Peso <= 0)
            {
                throw new ArgumentException($"El evento '{evento.Nombre}' debe tener un peso mayor que cero.");
            }

            if (evento.ImpactoMinimo > evento.ImpactoMaximo)
            {
                throw new ArgumentException($"En el evento '{evento.Nombre}', el impacto mínimo supera al máximo.");
            }

            if (evento.InfluenciaDeLaRacha < -1 || evento.InfluenciaDeLaRacha > 1)
            {
                throw new ArgumentException($"En el evento '{evento.Nombre}', la influencia de la racha debe estar entre -1 y 1.");
            }
        }

        ProbabilidadBaseDiaria = probabilidadBaseDiaria;
    }

    // Probabilidad de que ocurra un evento en el próximo tick, con los ajustes por tiempo y racha.
    public double CalcularProbabilidadDeEvento()
    {
        // De x1 a x2 a medida que pasa el tiempo sin eventos.
        var ajustePorTiempo = 1 + Math.Min((double)TicksSinEvento / TicksParaDuplicarProbabilidad, 1);

        // De x1 a x1,1 según qué tan fuerte sea la racha, para arriba o para abajo.
        var intensidadDeLaRacha = Math.Min(Math.Abs(Racha) / RachaDeReferencia, 1);
        var ajustePorRacha = 1 + InfluenciaMaximaDeLaRacha * intensidadDeLaRacha;

        return ProbabilidadBaseDiaria * ajustePorTiempo * ajustePorRacha;
    }

    // Al final de cada tick: puede ocurrir un evento, y después se actualiza la racha.
    protected override void AlTerminarElTick(double precioAlInicioDelTick, Random azar)
    {
        EventoDelUltimoTick = null;

        if (azar.NextDouble() < CalcularProbabilidadDeEvento())
        {
            var definicion = ElegirEvento(azar);
            var impacto = definicion.ImpactoMinimo
                        + azar.NextDouble() * (definicion.ImpactoMaximo - definicion.ImpactoMinimo);

            AplicarRendimiento(impacto / 100.0);
            EventoDelUltimoTick = new EventoOcurrido(definicion.Nombre, impacto);
            TicksSinEvento = 0;
        }
        else
        {
            TicksSinEvento++;
        }

        var rendimientoDelTick = PrecioActual / precioAlInicioDelTick - 1;
        Racha = FactorDeMemoria * rendimientoDelTick + (1 - FactorDeMemoria) * Racha;
    }

    // Sortea qué evento ocurre, según los pesos ajustados por la racha.
    private DefinicionDeEvento ElegirEvento(Random azar)
    {
        // Entre -1 (viene cayendo fuerte) y 1 (viene subiendo fuerte).
        var direccionDeLaRacha = Math.Clamp(Racha / RachaDeReferencia, -1, 1);

        var pesos = _eventosPosibles
            .Select(evento => evento.Peso
                * (1 + InfluenciaMaximaDeLaRacha * evento.InfluenciaDeLaRacha * direccionDeLaRacha))
            .ToList();

        var sorteo = azar.NextDouble() * pesos.Sum();

        for (var i = 0; i < pesos.Count; i++)
        {
            sorteo -= pesos[i];
            if (sorteo < 0)
            {
                return _eventosPosibles[i];
            }
        }

        return _eventosPosibles[^1]; // Por si el redondeo deja un resto mínimo.
    }
}