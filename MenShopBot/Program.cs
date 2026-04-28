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
        private static readonly string connectionString = "Server=kietne;Database=ShopQuanA;Trusted_Connection=True;TrustServerCertificate=True;";

        static async Task Main(string[] args)
        {
            Console.Title = "MenShop - Auto Payment Bot";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=================================================");
            Console.WriteLine("    HỆ THỐNG BOT LẮNG NGHE THANH TOÁN MENSHOP    ");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();
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

            DateTimeOffset botStartTime = DateTimeOffset.Now;

            while (true)
            {
                try
                {

                    var notifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);

                    foreach (var toast in notifications)
                    {
                        if (toast.CreationTime < botStartTime)
                        {
                            continue;
                        }

                        string appName = toast.AppInfo.DisplayInfo.DisplayName;

                        if (appName.Contains("Phone Link") || appName.Contains("Liên kết điện thoại"))
                        {
                            var textElements = toast.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                            if (textElements != null)
                            {
                                string content = string.Join(" ", textElements.Select(t => t.Text)).ToUpper();
                                if (content.Contains("MENSHOP") && content.Contains("+"))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"\n[CÓ BIẾN ĐỘNG SỐ DƯ]: {content}");
                                    Console.ResetColor();
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
                await Task.Delay(3000);
            }
        }

        static void ProcessPayment(string content, uint notificationId, UserNotificationListener listener)
        {
            var orderMatch = Regex.Match(content, @"MENSHOP\s*(\d+)");
            if (!orderMatch.Success)
            {
                Console.WriteLine("   -> Bỏ qua: Không tìm thấy mã đơn hàng hợp lệ trong tin nhắn.");
                return;
            }
            int orderId = int.Parse(orderMatch.Groups[1].Value);
            var moneyMatch = Regex.Match(content, @"\+([0-9,\.]+)");
            if (!moneyMatch.Success)
            {
                Console.WriteLine("   -> Bỏ qua: Không trích xuất được số tiền được cộng.");
                return;
            }
            double transferAmount = double.Parse(moneyMatch.Groups[1].Value.Replace(",", "").Replace(".", ""));

            Console.WriteLine($"   -> Khách đang thanh toán cho Đơn hàng: #{orderId} - Số tiền nhận: {transferAmount:N0}đ");
            ProcessOrderInDatabase(orderId, transferAmount);
            listener.RemoveNotification(notificationId);
        }

        static void ProcessOrderInDatabase(int orderId, double amount)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string checkQuery = "SELECT IsPaid, TotalAmount FROM [Order] WHERE Id = @OrderId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@OrderId", orderId);
                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("   -> [THẤT BẠI] Đơn hàng không tồn tại trong hệ thống.");
                                Console.ResetColor();
                                return;
                            }

                            bool isPaid = Convert.ToBoolean(reader["IsPaid"]);
                            double totalAmount = Convert.ToDouble(reader["TotalAmount"]);
                            if (isPaid)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("   -> [BỎ QUA] Đơn hàng này ĐÃ ĐƯỢC THANH TOÁN thành công từ trước.");
                                Console.ResetColor();
                                return;
                            }

                            if (amount < totalAmount)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"   -> [TỪ CHỐI] Khách chuyển THIẾU TIỀN (Cần: {totalAmount:N0}đ - Nhận: {amount:N0}đ). Yêu cầu khách chuyển lại đúng số tiền!");
                                Console.ResetColor();
                                return;
                            }
                        }
                    }
                    string updateQuery = "UPDATE [Order] SET IsPaid = 1, OrderStatusId = 2 WHERE Id = @OrderId";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@OrderId", orderId);
                        updateCmd.ExecuteNonQuery();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"   -> [THÀNH CÔNG] Đã tự động gạch nợ cho đơn hàng #{orderId}!");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   -> [LỖI KẾT NỐI DB]: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}