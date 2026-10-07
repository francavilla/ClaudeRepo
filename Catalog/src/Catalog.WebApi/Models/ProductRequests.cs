using System.ComponentModel.DataAnnotations;

namespace Catalog.WebApi.Models
{
    // Contratti HTTP: separati dai command, così l'API può evolvere senza toccare l'Application.

    public class CreateProductRequest
    {
        [Required]
        public string Sku { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public decimal? Price { get; set; }
    }

    public class ChangePriceRequest
    {
        [Required]
        public decimal? Price { get; set; }
    }
}
