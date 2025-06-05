using Configurations;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Diagnostics;
using System.Runtime;

// Configuração inicial de logging para capturar erros de inicialização
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando aplicação Clean Architecture Microservice");

    var builder = CreateOptimizedWebApplicationBuilder(args);

    // Configuração de serviços seguindo Clean Architecture
    ConfigureServices(builder);

    var app = builder.Build();

    // Configuração do pipeline de middleware
    ConfigureMiddleware(app);

    // Informações de performance de inicialização
    LogStartupInfo(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha crítica na inicialização da aplicação");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Métodos locais para organização seguindo princípio SRP
static WebApplicationBuilder CreateOptimizedWebApplicationBuilder(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Otimizações de performance para o host
    builder.Host.UseConsoleLifetime();

    // Configuração otimizada do Kestrel
    builder.WebHost.ConfigureKestrel((context, serverOptions) =>
    {
        serverOptions.AddServerHeader = false;
        serverOptions.AllowSynchronousIO = false;

        // Limits para prevenir ataques DoS
        serverOptions.Limits.MaxConcurrentConnections = 1000;
        serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
        serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
        serverOptions.Limits.MinRequestBodyDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
            bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
        serverOptions.Limits.MinResponseDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
            bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));

        // Keep-alive timeout otimizado
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });

    // Configuração de logging estruturado
    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "CleanArchMicroservice")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();
    });

    return builder;
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;

    // Configuração principal seguindo Clean Architecture
    services.AddCleanArchitectureServices(configuration);

    // Configurações específicas do ambiente
    if (builder.Environment.IsDevelopment())
    {
        services.AddDeveloperExceptionPage();
    }

    // Configurações de serialização JSON otimizadas
    services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.WriteIndented = false; // Compacto para produção
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
        options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

    // Compressão de resposta
    services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "text/json" });
    });

    // CORS otimizado se necessário
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Rate limiting para proteção contra abuso
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("ApiPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 50;
        });
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    // Pipeline de middleware otimizado seguindo ordem de performance

    // Middleware de desenvolvimento
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Compressão de resposta (antes de tudo que gera conteúdo)
    app.UseResponseCompression();

    // Rate limiting
    app.UseRateLimiter();

    // Segurança básica
    app.UseHttpsRedirection();

    // CORS se necessário
    if (app.Environment.IsDevelopment())
    {
        app.UseCors();
    }

    // Configuração específica da aplicação
    app.UseWebApiPipeline();

    // Endpoint de saúde básico
    app.MapGet("/", () => new
    {
        Service = "Clean Architecture Microservice",
        Version = typeof(Program).Assembly.GetName().Version?.ToString(),
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow,
        Status = "Healthy"
    })
    .WithName("Root")
    .WithOpenApi()
    .ExcludeFromDescription();

    // Endpoint de informações da aplicação
    app.MapGet("/info", () => new
    {
        Application = new
        {
            Name = "Clean Architecture Microservice",
            Version = typeof(Program).Assembly.GetName().Version?.ToString(),
            Environment = app.Environment.EnvironmentName,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingSet = GC.GetTotalMemory(false),
            GCMode = GCSettings.IsServerGC ? "Server" : "Workstation"
        },
        Runtime = new
        {
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
        },
        Performance = new
        {
            TotalMemory = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            ThreadPoolThreads = ThreadPool.ThreadCount,
            ActiveTimers = Timer.ActiveCount
        }
    })
    .WithName("ApplicationInfo")
    .WithOpenApi()
    .RequireAuthorization(); // Proteger informações sensíveis
}

static void LogStartupInfo(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var environment = app.Environment;

    logger.LogInformation("=== Aplicação Iniciada ===");
    logger.LogInformation("Nome: Clean Architecture Microservice");
    logger.LogInformation("Versão: {Version}", typeof(Program).Assembly.GetName().Version);
    logger.LogInformation("Ambiente: {Environment}", environment.EnvironmentName);
    logger.LogInformation("Modo GC: {GCMode}", GCSettings.IsServerGC ? "Server" : "Workstation");
    logger.LogInformation("URLs: {URLs}", string.Join(", ", app.Urls));

    // Log de configurações ativas
    LogActiveFeatures(logger);

    logger.LogInformation("=== Aplicação Pronta ===");
}

static void LogActiveFeatures(ILogger logger)
{
    var features = new List<string>();

#if SqlServerCondition
    features.Add("SQL Server");
#elif PostgreSQLCondition
    features.Add("PostgreSQL");
#elif MongoDbCondition
    features.Add("MongoDB");
#endif

#if UseJwtAuth
    features.Add("JWT Authentication");
#endif

#if UseSwagger
    features.Add("Swagger/OpenAPI");
#endif

#if UseHealthChecks
    features.Add("Health Checks");
#endif

#if UseMetrics
    features.Add("Metrics/Telemetry");
#endif

#if KafkaCondition
    features.Add("Kafka Messaging");
#endif

#if RabbitMQCondition
    features.Add("RabbitMQ Messaging");
#endif

#if RedisCondition
    features.Add("Redis Cache");
#endif

#if UseResultPattern
    features.Add("Result Pattern");
#endif

    if (features.Any())
    {
        logger.LogInformation("Recursos Ativos: {Features}", string.Join(", ", features));
    }
}