using LucidMicro.BuildingBlocks.AspNetCore.ExceptionHandling;
using LucidMicro.BuildingBlocks.Cors.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.DependencyInjection;
using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.DependencyInjection;
using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.Options;
using LucidMicro.Gateway.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddLucidSerilog();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<LucidExceptionHandler>();
builder.Services.AddLucidHealthChecks();
builder.Services.AddLucidOpenTelemetry(
    builder.Configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));
builder.Services.AddLucidCors(
    builder.Configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName));
builder.Services.AddLucidGatewayReverseProxy(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseLucidSerilogRequestLogging();
app.UseLucidCors();

app.MapLucidHealthChecks();
app.MapReverseProxy();

app.Run();
