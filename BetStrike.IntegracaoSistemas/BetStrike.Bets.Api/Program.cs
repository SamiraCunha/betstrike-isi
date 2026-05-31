
using BetStrike.Bets.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração (appsettings + secrets, se quiseres)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables();

// Ler a connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registar o DbConnectionFactory, injetando IConfiguration ou a própria string
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddHttpClient("ResultadosApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrls:Resultados"]);
});

builder.Services.AddScoped<EstatisticasRepository>();
builder.Services.AddSingleton<KafkaProducer, KafkaProducer>();

// se o teu DbConnectionFactory receber IConfiguration no construtor,
// o ASP.NET Core passa o builder.Configuration automaticamente.

builder.Services.AddControllers();
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
