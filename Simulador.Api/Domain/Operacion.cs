namespace Simulador.Api.Domain;

// Una compra o venta realizada. Total: lo que salió (compra) o entró (venta) de efectivo, con la comisión incluida.
public record Operacion(
    long Tick,
    TipoDeOperacion Tipo,
    string Activo,
    decimal Cantidad,
    decimal Precio,
    decimal Comision,
    decimal Total);