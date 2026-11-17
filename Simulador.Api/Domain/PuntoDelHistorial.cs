namespace Simulador.Api.Domain;

// Foto de un día: el índice de precios y el precio de cada activo.
// Los precios están en el mismo orden que Mundo.Activos.
public record PuntoDelHistorial(long Tick, double IndiceDePrecios, IReadOnlyList<double> Precios);