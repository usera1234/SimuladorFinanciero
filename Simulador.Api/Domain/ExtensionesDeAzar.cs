namespace Simulador.Api.Domain;

// Agrega a Random la capacidad de generar números con distribución normal (forma de campana).
public static class ExtensionesDeAzar

{

    // Devuelve un número con media 0 y desvío 1: casi siempre entre -2 y 2, rara vez más lejos.
    // Usa el método de Box-Muller, que convierte dos números uniformes en uno normal.

    public static double SiguienteNormal(this Random azar)

    {

        var u1 = 1.0 - azar.NextDouble(); // Entre 0 (excluido) y 1, para evitar el logaritmo de 0.
        var u2 = azar.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

    }
}