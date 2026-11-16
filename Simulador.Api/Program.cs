using System.Text.Json.Serialization;
using Simulador.Api.Engine;

var builder = WebApplication.CreateBuilder(args);

// Los enum se envían como texto ("Accion") en lugar de número (2).
builder.Services.AddControllers()
    .AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Fuente de la hora real.
builder.Services.AddSingleton(TimeProvider.System);

// Un único gestor, que guarda todas las partidas en curso.
builder.Services.AddSingleton<GestorDePartidas>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Sirve la interfaz web (archivos de la carpeta wwwroot). index.html se abre por defecto.
app.UseDefaultFiles();  
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();