using System.Text;
using MediTrack.FollowUpService.API.Application.Internal.CommandServices;
using MediTrack.FollowUpService.API.Application.Internal.EventHandlers;
using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Infrastructure.BlobStorage;
using MediTrack.FollowUpService.API.Infrastructure.Cleanup;
using MediTrack.FollowUpService.API.Infrastructure.ExternalServices;
using MediTrack.FollowUpService.API.Infrastructure.Messaging;
using MediTrack.FollowUpService.API.Infrastructure.TemporaryStorage;
using MediTrack.FollowUpService.API.Infrastructure.Persistence;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Repositories;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using MediTrack.FollowUpService.API.Infrastructure.Security;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// JWT authentication -- valores reales vía user-secrets en desarrollo, vía
// variables de entorno en producción. Nunca en appsettings.json (ver README).
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt:Key es obligatorio")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer es obligatorio")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience es obligatorio")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Database context
builder.Services.AddDbContext<FollowUpDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    ));

// Dependency Injection for DDD layers
builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IMedicationQueryService, MedicationQueryService>();
builder.Services.AddScoped<INextPendingDoseQueryService, NextPendingDoseQueryService>();

// Respaldo de sincronización next-dose/medications (ver docs/next-dose-sync-fix.md):
// si la réplica local de Medication/DoseSchedule está vacía para un paciente
// (evento RabbitMQ "PrescriptionCreated" perdido), se completa consultando el
// endpoint público existente de Treatment-Service.
builder.Services.AddHttpClient<ITreatmentMedicationsClient, TreatmentMedicationsClient>(client =>
{
    var treatmentBaseUrl = builder.Configuration["TreatmentService:BaseUrl"] ?? "http://localhost:5162";
    client.BaseAddress = new Uri(treatmentBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<IMedicationReplicaSyncService, MedicationReplicaSyncService>();
builder.Services.AddScoped<IAdherenceHistoryQueryService, AdherenceHistoryQueryService>();
builder.Services.AddScoped<ILowStockMedicationQueryService, LowStockMedicationQueryService>();
builder.Services.AddScoped<MedicationResourceFromEntityAssembler>();
builder.Services.AddScoped<NextPendingDoseResourceFromEntityAssembler>();
builder.Services.AddScoped<AdherenceHistoryResourceFromEntityAssembler>();
builder.Services.AddScoped<LowStockMedicationResourceFromEntityAssembler>();
builder.Services.AddScoped<IMedicationComplianceRepository, MedicationComplianceRepository>();
builder.Services.AddScoped<IMedicationComplianceCommandService, MedicationComplianceCommandService>();
builder.Services.AddScoped<RecordComplianceCommandFromResourceAssembler>();
builder.Services.AddScoped<MedicationComplianceResourceFromEntityAssembler>();
// Patrón Outbox: los eventos se persisten en la misma BD que el cambio de
// dominio y se entregan a RabbitMQ en background (no se pierden si el broker
// está caído justo al publicar).
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddScoped<IAppointmentComplianceRepository, AppointmentComplianceRepository>();
builder.Services.AddScoped<IAppointmentComplianceCommandService, AppointmentComplianceCommandService>();
builder.Services.AddScoped<IAppointmentComplianceQueryService, AppointmentComplianceQueryService>();
builder.Services.AddScoped<IOfflineSyncQueueRepository, OfflineSyncQueueRepository>();
builder.Services.AddScoped<IOfflineSyncCommandService, OfflineSyncCommandService>();
builder.Services.AddScoped<IPrescriptionCreatedEventHandler, PrescriptionCreatedEventHandler>();
builder.Services.AddScoped<IAppointmentScheduledEventHandler, AppointmentScheduledEventHandler>();
builder.Services.AddScoped<IMedicationCancelledEventHandler, MedicationCancelledEventHandler>();
builder.Services.AddScoped<IMedicationUpdatedEventHandler, MedicationUpdatedEventHandler>();
builder.Services.AddHostedService<PrescriptionCreatedConsumer>();
builder.Services.AddHostedService<AppointmentScheduledConsumer>();
builder.Services.AddHostedService<MedicationEventsConsumer>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<R2BlobOptions>(
    builder.Configuration.GetSection(R2BlobOptions.SectionName));
builder.Services.AddSingleton<IBlobStorageService, R2BlobStorageService>();

// Flujo de validación de evidencia en video (MediTrack AI Validator — Prototype):
// almacenamiento temporal privado + limpieza periódica de videos vencidos (>24h).
builder.Services.AddSingleton<ITemporaryVideoStorage, LocalTemporaryVideoStorage>();
builder.Services.AddHostedService<StaleComplianceVideoCleanupService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();
    db.Database.Migrate(); 
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// no-op: commit de prueba para verificar auto-deploy de Render
app.Run();
