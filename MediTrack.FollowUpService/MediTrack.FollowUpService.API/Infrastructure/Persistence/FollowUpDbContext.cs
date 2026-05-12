
using MediTrack.FollowUpService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence;

public class FollowUpDbContext : DbContext
{
    public FollowUpDbContext(DbContextOptions<FollowUpDbContext> options) 
        : base(options) { }

    public DbSet<Medication> Medications { get; set; }
    public DbSet<DoseSchedule> DoseSchedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.ToTable("medications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Dose).HasColumnName("dose");
            entity.Property(e => e.FrequencyHours).HasColumnName("frequency_hour");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StockCount).HasColumnName("stock_count");
            entity.HasMany(e => e.Schedules)
                .WithOne(s => s.Medication)
                .HasForeignKey(s => s.MedicationId);
        });

        modelBuilder.Entity<DoseSchedule>(entity =>
        {
            entity.ToTable("dose_schedules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MedicationId).HasColumnName("medication_id");
            entity.Property(e => e.ScheduledTime).HasColumnName("scheduled_time");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });
    }
}