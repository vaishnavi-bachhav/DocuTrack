using DocuTrack.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuTrack.Infrastructure.Persistence.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentNumber).IsRequired().HasMaxLength(30);
            builder.Property(d => d.Title).IsRequired().HasMaxLength(150);
            builder.Property(d => d.Description).HasMaxLength(500);
            builder.Property(d => d.Owner).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Version).HasDefaultValue(1);

            builder.HasIndex(d => d.DocumentNumber).IsUnique();
            builder.HasIndex(d => d.Status);
            builder.HasIndex(d => d.Department);
            builder.HasIndex(d => d.CreatedAt);
        }
    }
}
