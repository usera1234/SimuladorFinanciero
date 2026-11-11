namespace Simulador.Api.Domain;

// Variable de la economía (inflación, devaluación, ánimo cripto, etc.) que cambia en cada tick.
// Se mueve al azar, con tendencia a volver a su valor normal.

public class Factor

{

    public string Nombre { get; }

    // Valor alrededor del cual se mueve el factor.

    public double ValorNormal { get; }

    // Cuánto puede variar por azar en cada tick.

    public double Volatilidad { get; }

    // Entre 0 y 1: qué tan rápido vuelve a su valor normal. 0 = nunca, 1 = de golpe.

    public double FuerzaDeRetorno { get; }

    public double ValorActual { get; private set; }

    public Factor(
        
        string nombre,
        double valorNormal,
        double volatilidad,
        double fuerzaDeRetorno,
        double? valorInicial = null)

    {

        if (string.IsNullOrWhiteSpace(nombre))

        {

            throw new ArgumentException("El factor necesita un nombre.", nameof(nombre));
        
        }

        if (volatilidad < 0)

        {

            throw new ArgumentOutOfRangeException(nameof(volatilidad), "La volatilidad no puede ser negativa.");
        
        }

        if (fuerzaDeRetorno < 0 || fuerzaDeRetorno > 1)

        {

            throw new ArgumentOutOfRangeException(nameof(fuerzaDeRetorno), "La fuerza de retorno debe estar entre 0 y 1.");
        
        }

        Nombre = nombre;
        ValorNormal = valorNormal;
        Volatilidad = volatilidad;
        FuerzaDeRetorno = fuerzaDeRetorno;
        ValorActual = valorInicial ?? valorNormal; // Si no se indica un valor inicial, arranca en el normal.

    }

    // Mueve el factor un tick: un tirón hacia su valor normal más un empujón al azar.

    public void Avanzar(Random azar)

    {

        var tiron = FuerzaDeRetorno * (ValorNormal - ValorActual);
        var empujon = Volatilidad * azar.SiguienteNormal();

        ValorActual += tiron + empujon;
    
    }

}