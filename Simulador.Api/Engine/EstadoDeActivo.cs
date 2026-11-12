using Simulador.Api.Domain;

namespace Simulador.Api.Engine;

// Foto de un activo en un momento dado. La variación está en porcentaje (12,5 = subió 12,5%).
public record EstadoDeActivo(string Nombre, TipoDeActivo Tipo, double Precio, double VariacionDesdeElInicio);