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
        }
    }
}
