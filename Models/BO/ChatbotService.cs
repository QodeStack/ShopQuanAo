using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace ShopQuanAo.BO
{
    public class ChatbotService
    {
        private readonly ProductService _productService;
        private readonly CartService _cartService;
        private readonly CheckoutService _checkoutService;
        private readonly List<string> _apiKeys;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IMemoryCache _cache;

        private const int RATE_LIMIT_SECONDS = 4;
        private const int MAX_TOTAL_PRODUCTS = 8;
        private const int CACHE_DURATION_MINUTES = 10;
        private const int HTTP_TIMEOUT_SECONDS = 15;
        private const int MAX_API_RETRIES = 3;

        private const string FUNC_SEARCH = "tim_kiem_san_pham";
        private const string FUNC_CART = "kiem_tra_gio_hang";

        private static int _currentKeyIndex = 0;

        public ChatbotService(
            ProductService productService,
            CartService cartService,
            CheckoutService checkoutService,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ChatbotService> logger,
            IMemoryCache cache)
        {
            _productService = productService;
            _cartService = cartService;
            _checkoutService = checkoutService;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _apiKeys = configuration.GetSection("Gemini:ApiKeys").Get<List<string>>()
                       ?? throw new ArgumentNullException("Chưa cấu hình danh sách ApiKeys");
        }

        public async Task<string> ProcessChatAsync(ChatRequestDto request, string? userId, string clientIdentifier)
        {
            string userKey = string.IsNullOrEmpty(userId) ? $"anon_{clientIdentifier}" : userId;
            string rateLimitKey = $"RateLimit_{userKey}";

            if (_cache.TryGetValue(rateLimitKey, out _))
                return $"Bạn nhắn nhanh quá! Đợi mình {RATE_LIMIT_SECONDS} giây nhé 😅";

            _cache.Set(rateLimitKey, true, TimeSpan.FromSeconds(RATE_LIMIT_SECONDS));

            try
            {
                var payload = await BuildGeminiPayloadAsync(request, userId);
                var (doc, errorMsg) = await CallGeminiApiWithRetryAsync(payload);

                if (doc == null)
                    return $"Hệ thống AI đang bận (Chi tiết: {errorMsg}). Bạn đợi xíu nhé! 😅";

                return await ProcessGeminiResponseAsync(doc, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng: {Msg}", ex.Message);
                return "Ui, mạng bên mình đang hơi chập chờn, xíu nữa nhắn lại nha!";
            }
        }

        private async Task<object> BuildGeminiPayloadAsync(ChatRequestDto request, string? userId)
        {
            var categoriesMap = await _cache.GetOrCreateAsync("Bot_Categories", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);
                return await _productService.GetAvailableCategoriesAsync();
            });
            categoriesMap ??= new Dictionary<int, string>();

            string categoryLinks = string.Join(", ", categoriesMap.Select(c => $"{c.Value} (/Product?categoryId={c.Key})"));

            var availableSizes = await _cache.GetOrCreateAsync("Bot_Sizes", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);
                return await _productService.GetAvailableSizesAsync();
            });
            string sizeString = string.Join(", ", availableSizes ?? new List<string> { "S", "M", "L", "XL" });

            var publicVouchers = await _cache.GetOrCreateAsync("Bot_Vouchers", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);
                var rawVouchers = await _checkoutService.GetActiveVouchersAsync();
                return rawVouchers.Where(v => v.IsActive && v.IsPublic).ToList();
            });
            string promoString = publicVouchers != null && publicVouchers.Any()
                ? string.Join("; ", publicVouchers.Select(v => $"Mã '{v.Code}' (giảm {v.DiscountAmount:N0}đ cho đơn từ {v.MinOrderAmount:N0}đ)"))
                : "Hiện tại shop đang tạm hết mã giảm giá.";

            // ----------------------------------------------------------------------------------
            // 🧠 TÍNH NĂNG KÝ ỨC (USER CONTEXT): LẤY LỊCH SỬ MUA HÀNG GẦN NHẤT
            // ----------------------------------------------------------------------------------
            string userHistoryContext = "Khách hàng mới, chưa có lịch sử mua hàng.";
            if (!string.IsNullOrEmpty(userId))
            {
                var lastOrder = await _checkoutService.GetLatestOrderAsync(userId);
                if (lastOrder != null && lastOrder.OrderDetails != null && lastOrder.OrderDetails.Any())
                {
                    var lastItem = lastOrder.OrderDetails.First();
                    userHistoryContext = $"Khách từng mua {lastItem.Product.ProductName} (Size: {lastItem.Size}). Hãy dùng thông tin này để tư vấn size tương tự nếu khách không hỏi.";
                }
            }

            string shopInfo = "MenShop (Đà Nẵng). Hotline: 0865 306 765. Sáng lập: KQT Team.";

            string sysPrompt = $@"Bạn là Stylist AI của MenShop. Quy tắc:
1. TOOL: Tìm đồ -> '{FUNC_SEARCH}'. Xem giỏ -> '{FUNC_CART}'.
2. ĐIỀU HƯỚNG: Chỉ xin link -> KHÔNG gọi tool. Trả thẻ HTML <a href='/URL' style='color:#e00000; font-weight:bold;'>Tên</a>.
   - Link: Đăng nhập (/Identity/Account/Login), Sale (/Product/Sale), Lịch sử đơn (/Customer).
   - Danh mục: {categoryLinks}.
3. TƯ DUY: {userHistoryContext}
   - Tìm nhiều món: dùng dấu phẩy (VD: 'áo:1, quần:1').
   - Hàng HOT/Bán chạy: is_bestseller = true.
   - Size từ kho: [{sizeString}] (Dùng CHỮ).
4. INFO: Shop: {shopInfo}. Voucher: {promoString}.
5. KHÔNG DÙNG MARKDOWN. Trả lời dưới 50 chữ.";

            var mergedHistory = new List<(string Role, string Text)>();
            foreach (var msg in request.History.TakeLast(6))
            {
                string role = msg.Role == "user" ? "user" : "model";
                if (mergedHistory.Count > 0 && mergedHistory.Last().Role == role)
                    mergedHistory[mergedHistory.Count - 1] = (role, mergedHistory.Last().Text + "\n" + msg.Text);
                else
                    mergedHistory.Add((role, msg.Text));
            }
            if (mergedHistory.Count > 0 && mergedHistory.Last().Role == "user")
                mergedHistory[mergedHistory.Count - 1] = ("user", mergedHistory.Last().Text + "\n" + request.Text);
            else
                mergedHistory.Add(("user", request.Text));
            if (mergedHistory.Count > 0 && mergedHistory[0].Role == "model") mergedHistory.RemoveAt(0);

            return new
            {
                system_instruction = new { parts = new[] { new { text = sysPrompt } } },
                contents = mergedHistory.Select(h => new { role = h.Role, parts = new[] { new { text = h.Text } } }).ToArray(),
                tools = new[] {
                    new {
                        function_declarations = new object[] {
                            new {
                                name = FUNC_SEARCH,
                                description = "Tìm sản phẩm",
                                parameters = new {
                                    type = "OBJECT",
                                    properties = new Dictionary<string, object> {
                                        { "search", new { type = "STRING", description = "Món đồ khách cần" } },
                                        { "size", new { type = "STRING", description = $"Size chữ: {sizeString}" } },
                                        { "max_price", new { type = "NUMBER", description = "Giá tối đa (-1 nếu không giới hạn)" } },
                                        { "is_cheapest", new { type = "BOOLEAN", description = "Rẻ nhất" } },
                                        { "is_bestseller", new { type = "BOOLEAN", description = "Hàng bán chạy/hot" } }
                                    },
                                    required = new[] { "search" }
                                }
                            },
                            new { name = FUNC_CART, description = "Xem giỏ", parameters = new { type = "OBJECT", properties = new Dictionary<string, object>() } }
                        }
                    }
                }
            };
        }

        private async Task<(JsonDocument? Doc, string ErrorMsg)> CallGeminiApiWithRetryAsync(object payload)
        {
            string payloadJson = JsonSerializer.Serialize(payload);
            string lastError = "Lỗi kết nối API";
            for (int i = 0; i < Math.Min(MAX_API_RETRIES, _apiKeys.Count); i++)
            {
                int index = Interlocked.Increment(ref _currentKeyIndex) % _apiKeys.Count;
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKeys[index]}";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS));
                try
                {
                    var response = await _httpClient.PostAsync(endpoint, new StringContent(payloadJson, Encoding.UTF8, "application/json"), cts.Token);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(jsonString);
                    if (!doc.RootElement.TryGetProperty("error", out var err)) return (doc, "");
                    lastError = err.GetProperty("message").GetString() ?? "Lỗi API";
                    if (err.TryGetProperty("code", out var code) && code.GetInt32() == 400) break;
                }
                catch (Exception ex) { lastError = ex.Message; }
            }
            return (null, lastError);
        }

        private async Task<string> ProcessGeminiResponseAsync(JsonDocument doc, string? userId)
        {
            var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("functionCall", out var fc))
                {
                    string name = fc.GetProperty("name").GetString() ?? "";
                    if (name == FUNC_SEARCH) return await HandleProductSearchAsync(fc);
                    if (name == FUNC_CART) return await HandleCartCheckAsync(userId);
                }
            }
            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray()) if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());
            return sb.ToString().Trim();
        }

        private async Task<string> HandleProductSearchAsync(JsonElement fc)
        {
            string keyword = "áo:1", color = "", size = "";
            double maxPrice = -1; bool isCheapest = false, isBestseller = false;

            if (fc.TryGetProperty("args", out var args))
            {
                if (args.TryGetProperty("search", out var s)) keyword = s.GetString() ?? keyword;
                if (args.TryGetProperty("size", out var sz)) size = sz.GetString() ?? "";
                if (args.TryGetProperty("max_price", out var p) && p.ValueKind == JsonValueKind.Number) maxPrice = p.GetDouble();
                if (args.TryGetProperty("is_cheapest", out var ch)) isCheapest = ch.GetBoolean();
                if (args.TryGetProperty("is_bestseller", out var bs)) isBestseller = bs.GetBoolean();
            }

            string safeKeyword = keyword.Replace(" và ", ",").Replace(" & ", ",").Replace(" + ", ",");
            var keywords = safeKeyword.Split(',').Select(k => k.Trim().ToLower()).Where(k => !string.IsNullOrEmpty(k)).ToList();
            var topProducts = new List<Product>();
            double remainingBudget = maxPrice;
            int requestedItemsCount = 0;

            foreach (var kwItem in keywords)
            {
                string itemName = kwItem;
                int userRequestedQty = 0;

                if (kwItem.Contains(":"))
                {
                    string[] parts = kwItem.Split(':');
                    itemName = parts[0].Trim();
                    int.TryParse(parts[1].Trim(), out userRequestedQty);
                }

                int takeQty;
                if (userRequestedQty > 0)
                {
                    takeQty = userRequestedQty;
                }
                else
                {
                    if (keywords.Count > 1)
                    {
                        takeQty = 1;
                    }
                    else
                    {
                        takeQty = 4;
                    }
                }

                requestedItemsCount += takeQty;
                var candidates = await _productService.GetCandidatesForAIAsync(itemName, color, 0);

                if (maxPrice >= 0)
                {
                    double budgetToCompare = remainingBudget > 0 ? remainingBudget : maxPrice;
                    candidates = candidates.Where(p =>
                    {
                        double currentPrice = p.SalePrice > 0 ? p.SalePrice : p.Price;
                        return currentPrice <= budgetToCompare;
                    }).ToList();
                }

                if (isBestseller)
                {
                    candidates = candidates.OrderBy(p => p.Id).ToList();
                }
                else if (isCheapest)
                {
                    candidates = candidates.OrderBy(p => (p.SalePrice > 0 ? p.SalePrice : p.Price)).ToList();
                }
                else
                {
                    candidates = candidates.OrderByDescending(p => p.Id).ToList();
                }

                var pickedItems = candidates.Take(takeQty).ToList();
                topProducts.AddRange(pickedItems);

                if (maxPrice >= 0)
                {
                    foreach (var p in pickedItems)
                    {
                        double priceToPay = p.SalePrice > 0 ? p.SalePrice : p.Price;
                        remainingBudget -= priceToPay;
                    }
                }
            }

            bool isBudgetTooLow = (maxPrice >= 0 && topProducts.Count < requestedItemsCount);
            if (isBudgetTooLow)
            {
                topProducts.Clear();
                foreach (var kwItem in keywords)
                {
                    var candidates = await _productService.GetCandidatesForAIAsync(kwItem, color, 0);
                    topProducts.AddRange(candidates.OrderBy(p => p.SalePrice > 0 ? p.SalePrice : p.Price).Take(1));
                }
            }

            topProducts = topProducts.GroupBy(p => p.Id).Select(g => g.First()).Take(MAX_TOTAL_PRODUCTS).ToList();
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(size)) sb.AppendLine($"💡 <i>Gợi ý: Size <b>{size}</b> sẽ chuẩn nhất với bạn!</i><br/><br/>");

            bool isFallback = !topProducts.Any();
            if (isFallback)
            {
                topProducts = (await _productService.GetCandidatesForAIAsync("áo")).Take(4).ToList();
                sb.AppendLine("Mẫu bạn tìm hiện đang cháy hàng, xem thử các gợi ý hot này nhé:<br/><br/>");
            }
            else if (isBudgetTooLow)
            {
                sb.AppendLine($"Ngân sách {maxPrice:N0}đ hơi eo hẹp để mua trọn bộ combo này 😅. Mình đề xuất các lựa chọn rẻ nhất cho bạn nhé:<br/><br/>");
            }
            else if (isBestseller)
            {
                sb.AppendLine("Đây là các mẫu <b>Bán Chạy Nhất (Best-Seller)</b> đang cực hot tại MenShop:<br/><br/>");
            }
            else
            {
                sb.AppendLine("Dưới đây là các món đồ cực chuẩn mình đã tìm được cho bạn:<br/><br/>");
            }

            // ----------------------------------------------------------------------------------
            // 🖼️ RICH UI: THÊM ẢNH SẢN PHẨM VÀO KẾT QUẢ TRẢ VỀ
            // ----------------------------------------------------------------------------------
            double total = 0;
            foreach (var p in topProducts)
            {
                double price = p.SalePrice > 0 ? p.SalePrice : p.Price;
                total += price;
                string safeName = HtmlEncoder.Default.Encode(p.ProductName);
                sb.AppendLine("<div style='margin-bottom: 15px; display: flex; align-items: center;'>");
                sb.AppendLine($"  <img src='/Image/Product_image/{p.Image}' style='width: 50px; height: 50px; border-radius: 5px; margin-right: 10px; object-fit: cover;' />");
                sb.AppendLine("  <div>");
                sb.AppendLine($"    <a href='/Product/ProductDetail/{p.Id}' style='color:#e00000; font-weight:bold;'>{safeName}</a><br/>");
                sb.AppendLine($"    <span>{price:N0}đ</span>");
                sb.AppendLine("  </div>");
                sb.AppendLine("</div>");
            }

            if (!isFallback && topProducts.Count > 1 && !isBestseller) sb.AppendLine($"<br/><b>Tổng combo: {total:N0}đ</b><br/>");
            sb.AppendLine("<br/>Bạn có ưng mẫu nào không?");
            return sb.ToString();
        }

        private async Task<string> HandleCartCheckAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Bạn cần <a href='/Identity/Account/Login' style='color:#e00000; font-weight:bold;'>Đăng nhập</a> để xem giỏ hàng!";
            var cart = await _cartService.GetCartAsync(userId);
            if (cart == null || !cart.CartDetails.Any()) return "Giỏ hàng của bạn đang trống.";
            var sb = new StringBuilder("Giỏ hàng của bạn có:<br/><br/>");
            foreach (var item in cart.CartDetails) sb.AppendLine($"- <b>{HtmlEncoder.Default.Encode(item.Product.ProductName)}</b> (SL: {item.Quantity})<br/>");
            sb.AppendLine("<br/><a href='/Cart' style='color:#e00000; font-weight:bold;'>Vào Giỏ hàng</a>");
            return sb.ToString();
        }
    }
}