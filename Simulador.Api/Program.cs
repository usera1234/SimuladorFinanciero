using Simulador.Api.Domain;
using Simulador.Api.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Fuente de la hora real.
builder.Services.AddSingleton(TimeProvider.System);

// Un único mundo, con sus factores iniciales (valores provisorios).
builder.Services.AddSingleton(servicios => new Mundo(
    new[]
    {
        new Factor("Inflación mensual (%)", valorNormal: 2.0, volatilidad: 0.1, fuerzaDeRetorno: 0.02),
        new Factor("Devaluación mensual (%)", valorNormal: 1.5, volatilidad: 0.2, fuerzaDeRetorno: 0.03),
        new Factor("Ánimo cripto", valorNormal: 0.0, volatilidad: 1.0, fuerzaDeRetorno: 0.05),
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