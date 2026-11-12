using Simulador.Api.Domain;
using Simulador.Api.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Fuente de la hora real.
builder.Services.AddSingleton(TimeProvider.System);
// Nombres de los factores, para no escribirlos dos veces y evitar errores de tipeo.
const string Inflacion = "Inflación mensual (%)";
const string Devaluacion = "Devaluación mensual (%)";
const string AnimoCripto = "Ánimo cripto";
const string Actividad = "Actividad económica";

// Un único mundo, con sus factores iniciales (valores provisorios).
builder.Services.AddSingleton(servicios => new Mundo(
    new[]
    {
        new Factor(Inflacion, valorNormal: 2.0, volatilidad: 0.1, fuerzaDeRetorno: 0.02),
        new Factor(Devaluacion, valorNormal: 1.0, volatilidad: 0.2, fuerzaDeRetorno: 0.03),
        new Factor(AnimoCripto, valorNormal: 0.0, volatilidad: 1.0, fuerzaDeRetorno: 0.05),
        new Factor(Actividad, valorNormal: 0.0, volatilidad: 0.2, fuerzaDeRetorno: 0.02),
    },
    new[]
    {
        // El precio del plazo fijo representa cuánto vale cada peso depositado.
        new Activo("Plazo fijo", TipoDeActivo.Ahorro, precioInicial: 1,
            rendimientoBaseMensual: 2.6, volatilidadDiaria: 0,
            sensibilidades: new Dictionary<string, double>()),

        // Cotización oficial aproximada a noviembre de 2026.
        new Activo("Dólar", TipoDeActivo.Moneda, precioInicial: 1_450,
            rendimientoBaseMensual: 0, volatilidadDiaria: 0.4,
            sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0 }),

        // Precios de las empresas: inventados, porque las empresas son ficticias.
        new Activo("Energía del Sur", TipoDeActivo.Accion, precioInicial: 18_000,
            rendimientoBaseMensual: 0.3, volatilidadDiaria: 1.0,
            sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.9, [Actividad] = 0.5 }),

        new Activo("NubeTec", TipoDeActivo.Accion, precioInicial: 850,
            rendimientoBaseMensual: 0.5, volatilidadDiaria: 3.0,
            sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.5, [Actividad] = 1.5 }),

        new Activo("Constructora Ágil", TipoDeActivo.Accion, precioInicial: 4_200,
            rendimientoBaseMensual: 0, volatilidadDiaria: 2.0,
            sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.8, [Actividad] = 3.0 }),

        new Activo("AgroExporta", TipoDeActivo.Accion, precioInicial: 9_500,
            rendimientoBaseMensual: 0.2, volatilidadDiaria: 1.5,
            sensibilidades: new Dictionary<string, double> { [Inflacion] = 0.3, [Devaluacion] = 1.0, [Actividad] = 0.3 }),

        // Cripto: precio en dólares × precio inicial del dólar.
        new Activo("Bitcoin", TipoDeActivo.Cripto, precioInicial: 76_000 * 1_450,
            rendimientoBaseMensual: 0, volatilidadDiaria: 3.0,
            sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0, [AnimoCripto] = 1.0 }),

        new Activo("Ethereum", TipoDeActivo.Cripto, precioInicial: 2_000 * 1_450,
            rendimientoBaseMensual: 0, volatilidadDiaria: 4.5,
            sensibilidades: new Dictionary<string, double> { [Devaluacion] = 1.0, [AnimoCripto] = 1.5 }),
    },
    new Random()));

// Un único reloj, construido con el mundo y la fuente de la hora.
builder.Services.AddSingleton(servicios => new RelojDeSimulacion(
    servicios.GetRequiredService<Mundo>(),
    servicios.GetRequiredService<TimeProvider>()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();