using System.Text;
using AntigravitySaaS.Api.Infrastructure.Data;
using AntigravitySaaS.Api.Services;
using AntigravitySaaS.Api.Services.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// 1. Banco de Dados (PostgreSQL / SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=AntigravityDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<SaasDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Polly Resilience Policies (Circuit Breaker + Retry)
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutException>()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(
        3, 
        attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine($"[Polly Retry {retryAttempt}] Esperando {timespan.TotalSeconds}s devido a: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
        });

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// 3. Typed HttpClient para a IA
builder.Services.AddHttpClient<IAntigravityClient, AntigravityClient>(client =>
{
    var baseUrl = builder.Configuration["AiProvider:BaseUrl"] ?? "https://api.provedor-imagem.com/v1/";
    client.BaseAddress = new Uri(baseUrl);

    var apiKey = builder.Configuration["AiProvider:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    client.Timeout = TimeSpan.FromSeconds(60);
})
.AddPolicyHandler(circuitBreakerPolicy) // Circuit Breaker Externo
.AddPolicyHandler(retryPolicy);          // Retry Interno

// 4. HttpClient para Image Storage (Streaming)
builder.Services.AddHttpClient<IImageStorageService, ImageStorageService>();

// 5. Azure Blob Storage Client
builder.Services.AddSingleton(sp =>
{
    var blobConn = builder.Configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
    return new BlobServiceClient(blobConn);
});

// 6. Serviços de Domínio e Aplicação
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddSingleton<IPromptEngineService, AntigravityPromptEngineService>(); // Stateless 0ms

// 7. Autenticação JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ChaveSuperSecretaComMaisDeTrintaEDoisCaracteres123!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Antigravity API v1");
    c.RoutePrefix = string.Empty; // Abre direto ao acessar http://localhost:5000
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
