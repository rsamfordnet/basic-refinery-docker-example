using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<LoggingOpenTelemetryListener>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .WithTracing(tpb => tpb
        .ConfigureResource(r => r.AddService("demo-app"))
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
    );



var app = builder.Build();
var listener = app.Services.GetRequiredService<LoggingOpenTelemetryListener>();

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

public class LoggingOpenTelemetryListener : EventListener
{
    private readonly ILogger<LoggingOpenTelemetryListener> _logger;

    public LoggingOpenTelemetryListener(ILogger<LoggingOpenTelemetryListener> logger)
    {
        _logger = logger;
    }
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.StartsWith("OpenTelemetry"))
            EnableEvents(eventSource, EventLevel.Error);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        _logger.LogWarning(eventData.Message, eventData.Payload?.Select(p => p?.ToString())?.ToArray());
    }
}