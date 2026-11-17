namespace Simulador.Api.Domain;

// Tipos de movimiento de la billetera.
// PlazoFijo: el efectivo pasa a un depósito. CobroDePlazoFijo: el depósito vencido vuelve al efectivo con su interés.
public enum TipoDeOperacion
{
    Compra,
    Venta,
    PlazoFijo,
    CobroDePlazoFijo,
}