using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data.Configurations;

public class KbArticleConfiguration : IEntityTypeConfiguration<KbArticle>
{
    public void Configure(EntityTypeBuilder<KbArticle> builder)
    {
        builder.Property(a => a.Title).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(170).IsRequired();
        builder.HasIndex(a => a.Slug).IsUnique();

        builder.Property(a => a.BodyMarkdown).IsRequired();
        builder.Property(a => a.IsPublished).HasDefaultValue(false);
        builder.Property(a => a.ViewCount).HasDefaultValue(0);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(a => a.Category)
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The full-text GIN index over title + body is created with raw SQL in the
        // initial migration; EF cannot express an expression index of this shape.
    }
}
