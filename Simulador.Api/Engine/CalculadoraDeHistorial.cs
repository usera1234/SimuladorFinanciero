using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Lo que devuelve la API para dibujar gráficos. Son listas paralelas:
// la posición i de cada lista corresponde al día Ticks[i].
public record HistorialDePartida(
    IReadOnlyList<long> Ticks,
    IReadOnlyList<decimal> ValorTotal,
    IReadOnlyList<decimal> ValorReal,
    IReadOnlyList<SerieDeActivo> Activos);

// Precios de un activo en los mismos días que HistorialDePartida.Ticks.
public record SerieDeActivo(string Nombre, IReadOnlyList<double> Precios);

// Arma el historial de una partida. Es estática y no toca el reloj: recibe todo lo que necesita.
public static class CalculadoraDeHistorial
{
    // Elige como máximo "maximo" puntos repartidos de forma pareja, siempre con el primero y el último.
    public static List<PuntoDelHistorial> ElegirPuntos(IReadOnlyList<PuntoDelHistorial> historial, int maximo)
    {
        if (maximo < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maximo), "Se necesitan al menos 2 puntos.");
        }

        if (historial.Count <= maximo)
        {
            return historial.ToList();
        }

        var elegidos = new List<PuntoDelHistorial>(maximo);
        for (int i = 0; i < maximo; i++)
        {
            // i = 0 da el primer punto; i = maximo - 1 da el último.
            var posicion = (long)i * (historial.Count - 1) / (maximo - 1);
            elegidos.Add(historial[(int)posicion]);
        }

        return elegidos;
    }

    // Repite las operaciones sobre los puntos para saber cuánto valía la billetera cada día.
    // Los plazos fijos se suman aparte, con el interés ganado hasta cada día. Es opcional para no romper
    // las pruebas que ya existían.
    public static HistorialDePartida Calcular(
        IReadOnlyList<PuntoDelHistorial> puntos,
        IReadOnlyList<string> nombresDeActivos,
        decimal capitalInicial,
        IReadOnlyList<Operacion> operaciones,
        IReadOnlyList<PlazoFijo>? plazosFijos = null)
    {
        var efectivo = capitalInicial;
        var cantidades = new Dictionary<string, decimal>();
        var siguienteOperacion = 0;

        var ticks = new List<long>(puntos.Count);
        var valoresTotales = new List<decimal>(puntos.Count);
        var valoresReales = new List<decimal>(puntos.Count);

        foreach (var punto in puntos)
        {
            // Aplica las operaciones hechas hasta este día, inclusive. Están ordenadas por tick.
            while (siguienteOperacion < operaciones.Count && operaciones[siguienteOperacion].Tick <= punto.Tick)
            {
                var operacion = operaciones[siguienteOperacion];

                // Total ya incluye la comisión. Compra y plazo fijo sacan efectivo; venta y cobro lo devuelven.
                switch (operacion.Tipo)
                {
                    case TipoDeOperacion.Compra:
                        cantidades[operacion.Activo] = cantidades.GetValueOrDefault(operacion.Activo) + operacion.Cantidad;
                        efectivo -= operacion.Total;
                        break;
                    case TipoDeOperacion.Venta:
                        cantidades[operacion.Activo] = cantidades.GetValueOrDefault(operacion.Activo) - operacion.Cantidad;
                        efectivo += operacion.Total;
                        break;
                    case TipoDeOperacion.PlazoFijo:
                        efectivo -= operacion.Total;
                        break;
                    case TipoDeOperacion.CobroDePlazoFijo:
                        efectivo += operacion.Total;
                        break;
                }

                siguienteOperacion++;
            }

            var valor = efectivo;
            for (int i = 0; i < nombresDeActivos.Count; i++)
            {
                if (cantidades.TryGetValue(nombresDeActivos[i], out var cantidad))
                {
                    valor += cantidad * (decimal)punto.Precios[i];
                }
            }

            if (plazosFijos is not null)
            {
                valor += plazosFijos.Sum(plazoFijo => plazoFijo.ValorEn(punto.Tick));
            }

            ticks.Add(punto.Tick);
            valoresTotales.Add(Math.Round(valor, 2));
            valoresReales.Add(Math.Round(valor / (decimal)punto.IndiceDePrecios, 2));
        }

        var series = nombresDeActivos
            .Select((nombre, i) => new SerieDeActivo(nombre, puntos.Select(punto => punto.Precios[i]).ToList()))
            .ToList();

        return new HistorialDePartida(ticks, valoresTotales, valoresReales, series);
    }
}