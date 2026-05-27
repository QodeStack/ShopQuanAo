using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models.BEAN.Entity
{
    public class SaleCampaign
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CampaignName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<Product> Products { get; set; }
    }
}