using System;
using System.Collections.Generic;

namespace ShopQuanAo.Models.BEAN.DTO 
{
    public class CampaignDTO
    {
        public string CampaignName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<int> ProductIds { get; set; }

        public List<int> SalePrices { get; set; }
    }
    public class UpdateCampaignDto
    {
        public int Id { get; set; }
        public string CampaignName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> ProductIds { get; set; }
        public List<int> SalePrices { get; set; }
    }
}