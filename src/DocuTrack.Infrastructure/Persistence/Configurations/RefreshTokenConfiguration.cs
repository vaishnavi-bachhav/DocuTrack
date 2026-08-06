using DocuTrack.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuTrack.Infrastructure.Persistence.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            
            builder.ToTable("RefreshTokens", "security");
            builder.HasKey(rt => rt.Id);
            builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64);
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.UserId);
            builder.HasIndex(token => token.FamilyId);
            builder.Property(token => token.RevokedReason).HasMaxLength(200);
            builder.HasOne(token => token.User)
               .WithMany()
               .HasForeignKey(token => token.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
