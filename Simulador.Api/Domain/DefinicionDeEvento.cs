namespace Simulador.Api.Domain;

// Un tipo de evento que le puede pasar a una empresa.
// Peso: qué tan probable es frente a los otros eventos de la misma empresa.
// Impacto: rango del salto de precio, en porcentaje (-30 = cae 30%).
// InfluenciaDeLaRacha: entre -1 y 1. Positiva: más probable si la empresa viene subiendo.
// Negativa: más probable si viene bajando. Cero: no depende de la racha.
public record DefinicionDeEvento(
    string Nombre,
    double Peso,
    double ImpactoMinimo,
    double ImpactoMaximo,
    double InfluenciaDeLaRacha = 0);