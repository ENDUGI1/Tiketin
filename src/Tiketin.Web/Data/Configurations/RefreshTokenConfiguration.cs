using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.TokenHash);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.IsActive);
    }
}
