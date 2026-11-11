using Simulador.Api.Domain;
using Simulador.Api.Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Mundo>();
builder.Services.AddSingleton(servicios => new Mundo(
    new[]
    {
        new Factor("Inflación mensual (%)", valorNormal: 2.0, volatilidad: 0.1, fuerzaDeRetorno: 0.02),
        new Factor("Devaluación mensual (%)", valorNormal: 1.5, volatilidad: 0.2, fuerzaDeRetorno: 0.03),
        new Factor("Ánimo cripto", valorNormal: 0.0, volatilidad: 1.0, fuerzaDeRetorno: 0.05),
    },
    new Random()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
