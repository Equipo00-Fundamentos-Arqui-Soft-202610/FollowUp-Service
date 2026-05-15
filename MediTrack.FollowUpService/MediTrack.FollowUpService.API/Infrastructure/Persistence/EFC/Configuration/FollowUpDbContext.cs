using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;

public class FollowUpDbContext : DbContext
{
    public FollowUpDbContext(DbContextOptions<FollowUpDbContext> options) 
        : base(options) { }

    public DbSet<Medication> Medications { get; set; } = null!;
    public DbSet<DoseSchedule> DoseSchedules { get; set; } = null!;
    public DbSet<MedicationCompliance> MedicationCompliances { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Medication>(entity =>
        {
            entity.ToTable("medications");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.PatientId)
                .HasColumnName("patient_id")
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.Dose)
                .HasColumnName("dose")
                .HasMaxLength(100)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => new DoseValue(v));
            
            entity.Property(e => e.FrequencyHours)
                .HasColumnName("frequency_hour")
                .IsRequired();
            
            entity.Property(e => e.StartDate)
                .HasColumnName("start_date")
                .IsRequired();
            
            entity.Property(e => e.EndDate)
                .HasColumnName("end_date");
            
            entity.Property(e => e.StockCount)
                .HasColumnName("stock_count")
                .IsRequired();

            entity.HasMany(e => e.Schedules)
                .WithOne(s => s.Medication)
                .HasForeignKey(s => s.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DoseSchedule>(entity =>
        {
            entity.ToTable("dose_schedules");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.MedicationId)
                .HasColumnName("medication_id")
                .IsRequired();
            
            entity.Property(e => e.ScheduledTime)
                .HasColumnName("scheduled_time")
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => new ScheduledHour(v));
            
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
        });
        
        modelBuilder.Entity<MedicationCompliance>(entity =>
        {
            entity.ToTable("medication_compliances");
            entity.HasKey(e => e.Id);
    
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
    
            entity.Property(e => e.PatientId)
                .HasColumnName("patient_id")
                .IsRequired();
    
            entity.Property(e => e.DoseScheduleId)
                .HasColumnName("dose_schedule_id")
                .IsRequired();
    
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => ComplianceStatus.From(v));
    
            entity.Property(e => e.RecordedAt)
                .HasColumnName("recorded_at")
                .IsRequired();
    
            entity.Property(e => e.VideoUrl)
                .HasColumnName("video_url");
    
            entity.Property(e => e.Synced)
                .HasColumnName("synced")
                .HasDefaultValue(true);
    
            entity.Property(e => e.OfflineRecordedAt)
                .HasColumnName("offline_recorded_at");

            entity.HasOne(e => e.DoseSchedule)
                .WithMany()
                .HasForeignKey(e => e.DoseScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
    }
}