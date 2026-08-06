using LucidMicro.BuildingBlocks.AspNetCore.ExceptionHandling;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Cors.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.DependencyInjection;
using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.DependencyInjection;
using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.Options;
using LucidMicro.BuildingBlocks.OpenApi.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.OpenApi.AspNetCore.Options;
using LucidMicro.Services.Identity.Application.DependencyInjection;
using LucidMicro.Services.Identity.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddLucidSerilog();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<LucidExceptionHandler>();
builder.Services.AddLucidHealthChecks();
builder.Services.AddLucidOpenTelemetry(
    builder.Configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));
builder.Services.AddLucidOpenApi(
    builder.Configuration.GetRequiredSection(LucidOpenApiOptions.ConfigurationSectionName));
builder.Services.AddLucidCors(
    builder.Configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName));
builder.Services.AddLucidPermissionAuthorization();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseLucidSerilogRequestLogging();
app.UseLucidCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapLucidHealthChecks();
app.MapLucidOpenApi();
app.MapControllers();

app.Run();
