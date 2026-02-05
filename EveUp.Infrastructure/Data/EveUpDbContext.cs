using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Data;

public class EveUpDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public EveUpDbContext(DbContextOptions<EveUpDbContext> options)
        : base(options)
    {
    }

    public EveUpDbContext(
        DbContextOptions<EveUpDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobBreak> JobBreaks => Set<JobBreak>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Denunciation> Denunciations => Set<Denunciation>();
    public DbSet<Contestation> Contestations => Set<Contestation>();

    public override int SaveChanges()
    {
        NormalizeDateTimesToUtc();
        CreateAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        CreateAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                {
                    property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
            }
        }
    }

    private void CreateAuditLogs()
    {
        var auditableEntities = new[] { "User", "Job", "Payment", "Denunciation", "Application", "Dispute" };

        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && auditableEntities.Contains(e.Entity.GetType().Name)))
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = (Guid)entry.Property("Id").CurrentValue!;

            // Captura mudanças de estado se existir propriedade State
            var stateProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "State");
            string? previousState = null;
            string? newState = null;

            if (stateProperty != null)
            {
                previousState = stateProperty.OriginalValue?.ToString();
                newState = stateProperty.CurrentValue?.ToString();
            }

            // Só cria log se houve mudança de estado
            if (previousState != newState && newState != null)
            {
                var userId = _currentUserService?.UserId;
                var ipAddress = _currentUserService?.IpAddress;

                var auditLog = AuditLog.Create(
                    entityType,
                    entityId,
                    previousState,
                    newState,
                    "StateChange",
                    $"{entityType} state changed from {previousState} to {newState}",
                    userId,
                    ipAddress
                );

                AuditLogs.Add(auditLog);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== USER =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Cpf).HasMaxLength(11);
            entity.HasIndex(e => e.Cpf).IsUnique().HasFilter("\"Cpf\" IS NOT NULL");
            entity.Property(e => e.Cnpj).HasMaxLength(14);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.CompanyName).HasMaxLength(255);

            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.Property(e => e.Skills).HasColumnType("jsonb");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Availability).HasColumnType("jsonb");

            entity.Property(e => e.AverageRating).HasPrecision(3, 2);
            entity.Property(e => e.BanReason).HasMaxLength(500);
            entity.Property(e => e.PayeeId).HasMaxLength(255);

            entity.Property(e => e.EmailVerificationCode).HasMaxLength(6);

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasConversion<byte[]>();
        });

        // ===== REFRESH TOKEN =====
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token).IsRequired().HasMaxLength(512);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.TokenFamily).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TokenFamily);
            entity.Property(e => e.RevokedReason).HasMaxLength(255);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(512);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== JOB =====
        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RequiredSkills).HasColumnType("jsonb");

            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Latitude).HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasPrecision(10, 7);

            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(30).IsRequired();

            entity.Property(e => e.PaymentPerWorker).HasPrecision(10, 2);
            entity.Property(e => e.GrossFee).HasPrecision(10, 2);
            entity.Property(e => e.EveUpFeePercent).HasPrecision(5, 4);
            entity.Property(e => e.EveUpFee).HasPrecision(10, 2);
            entity.Property(e => e.NetFee).HasPrecision(10, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(12, 2);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.EventDate);

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasConversion<byte[]>();

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== APPLICATION =====
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.HasIndex(e => new { e.JobId, e.WorkerId }).IsUnique();

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Worker)
                .WithMany()
                .HasForeignKey(e => e.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== PAYMENT =====
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GrossAmount).HasPrecision(12, 2);
            entity.Property(e => e.PlatformFee).HasPrecision(10, 2);
            entity.Property(e => e.NetAmount).HasPrecision(12, 2);

            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(255);
            entity.HasIndex(e => e.TransactionId).IsUnique().HasFilter("\"TransactionId\" IS NOT NULL");
            entity.Property(e => e.PspResponse).HasColumnType("jsonb");
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");

            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(30).IsRequired();

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasConversion<byte[]>();

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== DISPUTE =====
        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.ToTable("disputes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.State).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Resolution).HasMaxLength(2000);
            entity.Property(e => e.Evidence).HasColumnType("jsonb");
            entity.Property(e => e.RefundAmount).HasPrecision(12, 2);
            entity.Property(e => e.WorkerPayout).HasPrecision(12, 2);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OpenedByUser)
                .WithMany()
                .HasForeignKey(e => e.OpenedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== REVIEW =====
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(1000);

            entity.HasIndex(e => new { e.JobId, e.ReviewerId }).IsUnique();

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== AUDIT LOG =====
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PreviousState).HasMaxLength(50);
            entity.Property(e => e.NewState).HasMaxLength(50);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ===== JOB BREAK =====
        modelBuilder.Entity<JobBreak>(entity =>
        {
            entity.ToTable("job_breaks");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Breaks)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== ATTENDANCE =====
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendances");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CheckInLatitude).HasPrecision(10, 7);
            entity.Property(e => e.CheckInLongitude).HasPrecision(10, 7);
            entity.Property(e => e.CheckOutLatitude).HasPrecision(10, 7);
            entity.Property(e => e.CheckOutLongitude).HasPrecision(10, 7);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.HasIndex(e => new { e.JobId, e.ProfessionalId }).IsUnique();

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CONVERSATION =====
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.JobId, e.ProfessionalId }).IsUnique();

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CHAT MESSAGE =====
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.AttachmentUrls).HasColumnType("jsonb");

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.SentAt);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== DENUNCIATION =====
        modelBuilder.Entity<Denunciation>(entity =>
        {
            entity.ToTable("denunciations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AttachmentUrls).HasColumnType("jsonb");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.Resolution).HasMaxLength(2000);

            entity.HasIndex(e => e.InitiatorId);
            entity.HasIndex(e => e.TargetId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasConversion<byte[]>();

            // SECURITY: Initiator navigation property removida para prevenir vazamento de identidade
            // FK existe no banco, mas sem navigation property no código para evitar .Include() acidental

            entity.HasOne(e => e.Target)
                .WithMany()
                .HasForeignKey(e => e.TargetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== CONTESTATION =====
        modelBuilder.Entity<Contestation>(entity =>
        {
            entity.ToTable("contestations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AttachmentUrls).HasColumnType("jsonb");

            entity.HasOne(e => e.Denunciation)
                .WithMany(d => d.Contestations)
                .HasForeignKey(e => e.DenunciationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contestant)
                .WithMany()
                .HasForeignKey(e => e.ContestantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
