using Simulador.Api.Domain;

namespace Simulador.Tests.Domain;

public class ExtensionesDeAzarTests
{
    [Fact]
    public void SiguienteNormal_EnMuchosNumeros_TieneMediaCeroYDesvioUno()
    {
        var azar = new Random(123);
        const int cantidad = 100_000;
        var numeros = new double[cantidad];

        for (var i = 0; i < cantidad; i++)
        {
            numeros[i] = azar.SiguienteNormal();
        }

        var media = numeros.Average();
        var desvio = Math.Sqrt(numeros.Average(x => (x - media) * (x - media)));

        Assert.InRange(media, -0.02, 0.02);
        Assert.InRange(desvio, 0.98, 1.02);
    }
}