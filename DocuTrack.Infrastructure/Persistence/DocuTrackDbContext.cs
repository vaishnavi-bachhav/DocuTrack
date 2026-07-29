using DocuTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DocuTrack.Core.Models;

namespace DocuTrack.Infrastructure.Persistence
{
    public sealed class DocuTrackDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public DocuTrackDbContext(DbContextOptions<DocuTrackDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("Users", "security");
            modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", "security");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "security");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "security");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "security");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "security");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "security");

            modelBuilder.HasSequence<long>("DocumentNumberSequence", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuTrackDbContext).Assembly);
        }
    }
}
