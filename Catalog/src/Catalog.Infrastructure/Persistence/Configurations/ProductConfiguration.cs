using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Catalog.Domain.Products;

namespace Catalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapping fluent: il Domain resta privo di attributi di persistenza.
    /// </summary>
    internal sealed class ProductConfiguration : EntityTypeConfiguration<Product>
    {
        public ProductConfiguration()
        {
            ToTable("Products");
            HasKey(p => p.Id);

            Property(p => p.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            Property(p => p.Sku)
                .IsRequired()
                .HasMaxLength(Product.SkuMaxLength)
                .IsUnicode(false);

            HasIndex(p => p.Sku)
                .IsUnique()
                .HasName("UX_Products_Sku");

            Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(Product.NameMaxLength);

            Property(p => p.Price).HasPrecision(18, 2);

            Property(p => p.CreatedOnUtc).HasColumnType("datetime2");
        }
    }
}
