using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications.Management;
using Windows.UI.Notifications;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace MenShopBot
{
    class Program
    {
        // QUAN TRỌNG: Sửa dòng này thành chuỗi kết nối Database thực tế của bạn (Copy từ appsettings.json của web)
        private static readonly string connectionString = "Server=kietne;Database=DatHangPbl;Trusted_Connection=True;TrustServerCertificate=True;";

        static async Task Main(string[] args)
        {
            Console.Title = "MenShop - Auto Payment Bot";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=================================================");
            Console.WriteLine("    HỆ THỐNG BOT LẮNG NGHE THANH TOÁN MENSHOP    ");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();

            // Xin quyền đọc thông báo từ hệ điều hành Windows
            var listener = UserNotificationListener.Current;
            var accessStatus = await listener.RequestAccessAsync();

            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[LỖI] Hệ thống chưa được cấp quyền đọc thông báo!");
                Console.WriteLine("Cách sửa: Mở Settings (Windows) -> Privacy & Security -> Notifications -> Bật 'Allow apps to access your notifications'.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("[*] Đã kết nối thành công. Đang chờ SMS từ Phone Link...\n");

            // ==============================================================
            // BỘ LỌC THỜI GIAN: Lưu lại thời điểm chính xác lúc Bot khởi động
            // ==============================================================
            DateTimeOffset botStartTime = DateTimeOffset.Now;

            // Vòng lặp vô hạn để canh gác thông báo 24/7
            while (true)
            {
                try
                {
                    // Lấy tất cả thông báo đang hiện trên máy tính
                    var notifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);

                    foreach (var toast in notifications)
                    {
                        // ==============================================================
                        // KIỂM TRA HẠN SỬ DỤNG: Nếu thông báo được tạo ra TRƯỚC khi bật Bot -> Lờ đi
                        // ==============================================================
                        if (toast.CreationTime < botStartTime)
                        {
                            continue;
                        }

                        string appName = toast.AppInfo.DisplayInfo.DisplayName;

                        // CHỈ bắt thông báo đến từ App Phone Link (Liên kết điện thoại)
                        if (appName.Contains("Phone Link") || appName.Contains("Liên kết điện thoại"))
                        {
                            var textElements = toast.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                            if (textElements != null)
                            {
                                string content = string.Join(" ", textElements.Select(t => t.Text)).ToUpper();

                                // Bộ lọc chống rác: Phải có chữ MENSHOP và dấu +
                                if (content.Contains("MENSHOP") && content.Contains("+"))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"\n[CÓ BIẾN ĐỘNG SỐ DƯ]: {content}");
                                    Console.ResetColor();

                                    // Xử lý gạch nợ
                                    ProcessPayment(content, toast.Id, listener);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[LỖI HỆ THỐNG]: {ex.Message}");
                    Console.ResetColor();
                }

                // Chờ 3 giây rồi quét tiếp để máy tính không bị giật lag
                await Task.Delay(3000);
            }
        }

        static void ProcessPayment(string content, uint notificationId, UserNotificationListener listener)
        {
            // 1. Tách mã đơn hàng bằng Regex (VD: MENSHOP 1234 -> lấy số 1234)
            var orderMatch = Regex.Match(content, @"MENSHOP\s*(\d+)");
            if (!orderMatch.Success)
            {
                Console.WriteLine("   -> Bỏ qua: Không tìm thấy mã đơn hàng hợp lệ trong tin nhắn.");
                return;
            }
            int orderId = int.Parse(orderMatch.Groups[1].Value);

            // 2. Tách số tiền thực nhận (VD: +500,000 -> lấy 500000)
            var moneyMatch = Regex.Match(content, @"\+([0-9,\.]+)");
            if (!moneyMatch.Success)
            {
                Console.WriteLine("   -> Bỏ qua: Không trích xuất được số tiền được cộng.");
                return;
            }
            double transferAmount = double.Parse(moneyMatch.Groups[1].Value.Replace(",", "").Replace(".", ""));

            Console.WriteLine($"   -> Khách đang thanh toán cho Đơn hàng: #{orderId} - Số tiền nhận: {transferAmount:N0}đ");

            // 3. Mở kết nối Database và cập nhật trạng thái đơn
            bool isSuccess = UpdateOrderInDatabase(orderId, transferAmount);

            if (isSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   -> [THÀNH CÔNG] Đã tự động gạch nợ cho đơn hàng #{orderId}!");
                Console.ResetColor();

                // Dọn dẹp: Xóa cái thông báo đó khỏi màn hình để vòng lặp sau không đọc lại nữa
                listener.RemoveNotification(notificationId);
            }
        }

        static bool UpdateOrderInDatabase(int orderId, double amount)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Logic Update SQL Server: Đơn chưa thanh toán VÀ Khách chuyển đủ/dư tiền
                    // QUAN TRỌNG: Hãy đảm bảo bảng Orders và các cột (IsPaid, OrderStatusId, TotalAmount) khớp với DB của bạn.
                    string query = @"
                        UPDATE [Order] 
                        SET IsPaid = 1, OrderStatusId = 2 
                        WHERE Id = @OrderId 
                          AND IsPaid = 0 
                          AND TotalAmount <= @Amount";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        cmd.Parameters.AddWithValue("@Amount", amount);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return true; // Gạch nợ OK
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("   -> [THẤT BẠI] Lỗi: Đơn không tồn tại, đã gạch nợ trước đó, hoặc khách chuyển thiếu tiền.");
                            Console.ResetColor();
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   -> [LỖI KẾT NỐI DB]: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
    }
}