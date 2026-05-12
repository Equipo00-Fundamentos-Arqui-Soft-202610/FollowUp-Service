using MediTrack.FollowUpService.API.Application.Internal.CommandServices;
using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Infrastructure.Persistence;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database context
builder.Services.AddDbContext<FollowUpDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    ));

// Dependency Injection for DDD layers
builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IMedicationQueryService, MedicationQueryService>();
builder.Services.AddScoped<MedicationResourceFromEntityAssembler>();
builder.Services.AddScoped<IMedicationComplianceRepository, MedicationComplianceRepository>();
builder.Services.AddScoped<IMedicationComplianceCommandService, MedicationComplianceCommandService>();
builder.Services.AddScoped<RecordComplianceCommandFromResourceAssembler>();
builder.Services.AddScoped<MedicationComplianceResourceFromEntityAssembler>();
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
app.UseAuthorization();
app.MapControllers();
app.Run();