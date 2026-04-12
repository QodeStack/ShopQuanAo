namespace ShopQuanAo.Models.DTO
{
    // --- User DTOs ---
    public class CreateUserDto { public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string? Role { get; set; } }
    public class EditUserDto { public string Id { get; set; } = ""; public string? Role { get; set; } }
    public class DeleteDto { public string Id { get; set; } = ""; }

    // --- Product DTOs ---
    public class ProductDto { public int Id { get; set; } public string? ProductName { get; set; } public string? BrandName { get; set; } public double Price { get; set; } public string? Image { get; set; } public int CategoryId { get; set; } }
    public class CreateProductWithSizesDto { public string ProductName { get; set; } = ""; public string? BrandName { get; set; } public double Price { get; set; } public string? Image { get; set; } public int CategoryId { get; set; } public List<SizeDto>? Sizes { get; set; } }
    public class SizeDto { public string SizeName { get; set; } = ""; public int Quantity { get; set; } }
    public class UpdateProductSizeDto { public int ProductSizeId { get; set; } public int Quantity { get; set; } }
    public class DeleteProductSizeDto { public int ProductSizeId { get; set; } }
    public class DeleteIntDto { public int Id { get; set; } }

    // --- Order & Contact DTOs ---
    public class UpdateOrderStatusDto { public int OrderId { get; set; } public string Action { get; set; } = ""; public string? ShippingProvider { get; set; } }
    public class ReplyContactDto { public int Id { get; set; } public string AdminReply { get; set; } = ""; }
}