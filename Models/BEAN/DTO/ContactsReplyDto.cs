namespace ShopQuanAo.Models.BEAN.DTO
{
	public class ContactsReplyDto
	{
		// ID của tin nhắn cần phản hồi
		public int Id { get; set; }

		// Nội dung mà Admin (Quốc) sẽ viết để trả lời khách
		public string AdminReply { get; set; }
	}
}