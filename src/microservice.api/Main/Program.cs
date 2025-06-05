using Configurations;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Diagnostics;
using System.Runtime;

// Configura��o inicial de logging para capturar erros de inicializa��o
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando aplica��o Clean Architecture Microservice");

    var builder = CreateOptimizedWebApplicationBuilder(args);

    // Configura��o de servi�os seguindo Clean Architecture
    ConfigureServices(builder);

    var app = builder.Build();

    // Configura��o do pipeline de middleware
    ConfigureMiddleware(app);

    // Informa��es de performance de inicializa��o
    LogStartupInfo(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha cr�tica na inicializa��o da aplica��o");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// M�todos locais para organiza��o seguindo princ�pio SRP
static WebApplicationBuilder CreateOptimizedWebApplicationBuilder(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Otimiza��es de performance para o host
    builder.Host.UseConsoleLifetime();

    // Configura��o otimizada do Kestrel
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

    // Configura��o de logging estruturado
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

    // Configura��o principal seguindo Clean Architecture
    services.AddCleanArchitectureServices(configuration);

    // Configura��es espec�ficas do ambiente
    if (builder.Environment.IsDevelopment())
    {
        services.AddDeveloperExceptionPage();
    }

    // Configura��es de serializa��o JSON otimizadas
    services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.WriteIndented = false; // Compacto para produ��o
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
        options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

    // Compress�o de resposta
    services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "text/json" });
    });

    // CORS otimizado se necess�rio
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Rate limiting para prote��o contra abuso
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

    // Compress�o de resposta (antes de tudo que gera conte�do)
    app.UseResponseCompression();

    // Rate limiting
    app.UseRateLimiter();

    // Seguran�a b�sica
    app.UseHttpsRedirection();

    // CORS se necess�rio
    if (app.Environment.IsDevelopment())
    {
        app.UseCors();
    }

    // Configura��o espec�fica da aplica��o
    app.UseWebApiPipeline();

    // Endpoint de sa�de b�sico
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

    // Endpoint de informa��es da aplica��o
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
    .RequireAuthorization(); // Proteger informa��es sens�veis
}

static void LogStartupInfo(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var environment = app.Environment;

    logger.LogInformation("=== Aplica��o Iniciada ===");
    logger.LogInformation("Nome: Clean Architecture Microservice");
    logger.LogInformation("Vers�o: {Version}", typeof(Program).Assembly.GetName().Version);
    logger.LogInformation("Ambiente: {Environment}", environment.EnvironmentName);
    logger.LogInformation("Modo GC: {GCMode}", GCSettings.IsServerGC ? "Server" : "Workstation");
    logger.LogInformation("URLs: {URLs}", string.Join(", ", app.Urls));

    // Log de configura��es ativas
    LogActiveFeatures(logger);

    logger.LogInformation("=== Aplica��o Pronta ===");
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