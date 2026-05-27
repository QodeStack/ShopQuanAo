using System;
using System.Collections.Generic;

namespace ShopQuanAo.Models.BEAN.DTO // Đảm bảo namespace này khớp với đường dẫn thư mục
{
    public class CampaignDTO
    {
        // Tên chiến dịch (Vd: "Xả kho mùa hè")
        public string CampaignName { get; set; }

        // Thời gian áp dụng
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Danh sách ID sản phẩm được chọn
        public List<int> ProductIds { get; set; }

        // Danh sách giá khuyến mãi tương ứng
        // Tao đổi thành int luôn cho khớp với thuộc tính SalePrice trong class Product của mày
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