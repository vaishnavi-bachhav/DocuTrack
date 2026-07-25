using DocuTrack.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.Persistence
{
    public sealed class DocuTrackDbContext : DbContext
    {
        public DocuTrackDbContext(DbContextOptions<DocuTrackDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.HasSequence<long>("DocumentNumberSequence", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuTrackDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
