using System.Text;
using MediTrack.FollowUpService.API.Application.Internal.CommandServices;
using MediTrack.FollowUpService.API.Application.Internal.EventHandlers;
using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Infrastructure.BlobStorage;
using MediTrack.FollowUpService.API.Infrastructure.Messaging;
using MediTrack.FollowUpService.API.Infrastructure.Persistence;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Repositories;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
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

// JWT authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Falta la clave de firma JWT en 'Jwt:Key'.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
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
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
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
builder.Services.Configure<AzureBlobOptions>(
    builder.Configuration.GetSection(AzureBlobOptions.SectionName));
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();
    db.Database.Migrate(); 
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
