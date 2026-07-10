using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Domain.Models;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;

public class FollowUpDbContext : DbContext
{
    public FollowUpDbContext(DbContextOptions<FollowUpDbContext> options)
        : base(options) { }

    public DbSet<Medication> Medications { get; set; } = null!;
    public DbSet<DoseSchedule> DoseSchedules { get; set; } = null!;
    public DbSet<MedicationCompliance> MedicationCompliances { get; set; } = null!;
    public DbSet<AppointmentCompliance> AppointmentCompliances { get; set; } = null!;
    public DbSet<OfflineSyncQueueItem> OfflineSyncQueueItems { get; set; } = null!;
    public DbSet<AppointmentReference> AppointmentReferences { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Medication>(entity =>
        {
            entity.ToTable("medications");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            
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

            entity.Property(e => e.StockAlertThreshold)
                .HasColumnName("stock_alert_threshold")
                .HasDefaultValue(0);

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
                .ValueGeneratedNever();
            
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

            entity.Property(e => e.ScheduledAt)
                .HasColumnName("scheduled_at");

            entity.Property(e => e.ValidatedAt)
                .HasColumnName("validated_at");

            entity.Property(e => e.ValidatorId)
                .HasColumnName("validator_id");

            entity.Property(e => e.RejectionReason)
                .HasColumnName("rejection_reason")
                .HasMaxLength(500);

            entity.Property(e => e.TemporaryVideoPath)
                .HasColumnName("temporary_video_path")
                .HasMaxLength(260);

            entity.Property(e => e.VideoExpiresAt)
                .HasColumnName("video_expires_at");

            entity.HasOne(e => e.DoseSchedule)
                .WithMany()
                .HasForeignKey(e => e.DoseScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<AppointmentCompliance>(entity =>
        {
            entity.ToTable("appointment_compliances");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.PatientId)
                .HasColumnName("patient_id")
                .IsRequired();

            entity.Property(e => e.AppointmentId)
                .HasColumnName("appointment_id")
                .IsRequired();

            entity.Property(e => e.Attended)
                .HasColumnName("attended")
                .IsRequired();

            entity.Property(e => e.RecordedAt)
                .HasColumnName("recorded_at")
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasColumnName("notes");
        });

        modelBuilder.Entity<OfflineSyncQueueItem>(entity =>
        {
            entity.ToTable("offline_sync_queue");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.PatientId)
                .HasColumnName("patient_id")
                .IsRequired();

            entity.Property(e => e.EntityType)
                .HasColumnName("entity_type")
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("json")
                .IsRequired();

            entity.Property(e => e.QueuedAt)
                .HasColumnName("queued_at")
                .IsRequired();

            entity.Property(e => e.SyncedAt)
                .HasColumnName("synced_at");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .HasDefaultValue("pending");
        });

        modelBuilder.Entity<AppointmentReference>(entity =>
        {
            entity.ToTable("appointment_reference");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.PatientId).HasColumnName("patient_id").IsRequired();
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at").IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("processed_events");

            entity.HasKey(e => e.EventId);

            entity.Property(e => e.EventId)
                .HasConversion(g => g.ToByteArray(), b => new Guid(b))
                .HasColumnType("binary(16)");

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ProcessedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_message");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Id)
                .HasConversion(g => g.ToByteArray(), b => new Guid(b))
                .HasColumnType("binary(16)");

            entity.Property(m => m.EventType).HasMaxLength(100).IsRequired();
            entity.Property(m => m.Payload).HasColumnType("json").IsRequired();
            entity.Property(m => m.OccurredAtUtc).IsRequired();
            entity.Property(m => m.LastError).HasMaxLength(500);

            entity.HasIndex(m => m.ProcessedAtUtc);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ResponseBody).HasColumnType("json").IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}