using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sycota.Domain.Entities;

namespace Sycota.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for the new entities
        public DbSet<Club> Clubs { get; set; }
        public DbSet<ClubMember> ClubMembers { get; set; }
        public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<ShooterProfile> ShooterProfiles { get; set; }
        public DbSet<ClubJoinRequest> ClubJoinRequests { get; set; }
        public DbSet<ClubInvitation> ClubInvitations { get; set; }
        public DbSet<AiChatMessage> AiChatMessages { get; set; }
        public DbSet<ClubAnnouncement> ClubAnnouncements { get; set; }
        public DbSet<ClubWeapon> ClubWeapons { get; set; }
        public DbSet<ClubAmmo> ClubAmmo { get; set; }
        public DbSet<InventoryIssue> InventoryIssues { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // Configure Club entity
            builder.Entity<Club>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.ContactEmail).HasMaxLength(256);
                entity.Property(e => e.ContactPhone).HasMaxLength(50);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ClubMember entity
            builder.Entity<ClubMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ClubId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.Members)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.Competitors)
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure TrainingSession entity
            builder.Entity<TrainingSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.TrainingSessions)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ShooterProfile entity (ISSF 10m specific)
            builder.Entity<ShooterProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ClubMemberId).IsUnique();

                entity.HasOne(e => e.ClubMember)
                    .WithOne(cm => cm.ShooterProfile)
                    .HasForeignKey<ShooterProfile>(e => e.ClubMemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                // License information
                entity.Property(e => e.ISSFLicenseNumber).HasMaxLength(100);
                entity.Property(e => e.NationalLicenseNumber).HasMaxLength(100);
                entity.Property(e => e.MedicalCertificateNumber).HasMaxLength(100);
                
                entity.Property(e => e.AdditionalNotes).HasMaxLength(2000);
                
                // Enum conversions
                entity.Property(e => e.PrimaryWeapon).HasConversion<int>();
                entity.Property(e => e.Category).HasConversion<int>();
            });

            // Configure ClubJoinRequest entity
            builder.Entity<ClubJoinRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ClubId, e.Status });

                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
                entity.Property(e => e.RequestedRole).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.JoinRequests)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RequestedTrainer)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedTrainerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ProcessedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ProcessedById)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure ClubInvitation entity
            builder.Entity<ClubInvitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvitationCode).IsUnique();
                entity.HasIndex(e => new { e.ClubId, e.Email, e.Status });

                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.InvitationCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.OfferedRole).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.Invitations)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.AssignedTrainer)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedTrainerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.AcceptedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.AcceptedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure AiChatMessage entity
            builder.Entity<AiChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TrainingSessionId, e.CreatedAt });

                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired();

                entity.HasOne(e => e.TrainingSession)
                    .WithMany()
                    .HasForeignKey(e => e.TrainingSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ClubAnnouncement entity
            builder.Entity<ClubAnnouncement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.CreatedByName).IsRequired().HasMaxLength(200);

                entity.HasIndex(e => new { e.ClubId, e.CreatedAt });

                entity.HasOne<Club>()
                    .WithMany(c => c.Announcements)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ClubWeapon entity
            builder.Entity<ClubWeapon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClubId, e.SerialNumber }).IsUnique();
                entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Model).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.Weapons)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedShooter)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedShooterId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure ClubAmmo entity
            builder.Entity<ClubAmmo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClubId, e.SerialNumber }).IsUnique();
                entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).HasConversion<int>();

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.AmmoBatches)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure InventoryIssue entity
            builder.Entity<InventoryIssue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClubId, e.IssuedAt });

                entity.HasOne(e => e.Club)
                    .WithMany(c => c.InventoryIssues)
                    .HasForeignKey(e => e.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Shooter)
                    .WithMany()
                    .HasForeignKey(e => e.ShooterId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.IssuedBy)
                    .WithMany()
                    .HasForeignKey(e => e.IssuedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Weapon)
                    .WithMany()
                    .HasForeignKey(e => e.WeaponId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Ammo)
                    .WithMany()
                    .HasForeignKey(e => e.AmmoId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
