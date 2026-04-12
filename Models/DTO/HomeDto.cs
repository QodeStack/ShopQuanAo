using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models.DTO
{
    public class ContactFormDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; } = "";
    }
}