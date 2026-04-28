using System.Collections.Generic;

namespace ShopQuanAo.Models.BEAN.DTO
{
    // Lớp để lưu trữ từng dòng hội thoại trong lịch sử
    public class ChatMessage
    {
        public string Role { get; set; } = ""; // "user" hoặc "model"
        public string Text { get; set; } = "";
    }

    // DTO nhận dữ liệu từ giao diện gửi lên
    public class ChatRequestDto
    {
        public string Text { get; set; } = "";
        public string? ImageBase64 { get; set; }
        public List<ChatMessage> History { get; set; } = new();
    }
}