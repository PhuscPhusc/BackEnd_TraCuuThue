using api_thue.Data;
using api_thue.DTOs;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net;

namespace api_thue.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class doanhNghiepController : ControllerBase
    {

        // Lưu sessionId -> CookieContainer
        private static readonly Dictionary<string, CookieContainer> _sessions = new();
        private readonly ApplicationDbContext _db;

        public doanhNghiepController(IConfiguration config)
        {
            _db = new ApplicationDbContext(config);
        }
        [HttpGet("captcha")]
        public async Task<IActionResult> GetCaptchaAsync()
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler);

            var captchaUrl = $"https://tracuunnt.gdt.gov.vn/tcnnt/captcha.png?uid={Guid.NewGuid()}";
            var imgBytes = await client.GetByteArrayAsync(captchaUrl);

            // Tạo sessionId và lưu cookieContainer
            var sessionId = Guid.NewGuid().ToString();
            _sessions[sessionId] = cookieContainer;
            Console.WriteLine($"[Captcha] Tạo sessionId={sessionId}, CookieCount={cookieContainer.Count}");
            
            Response.Headers["X-Session-Id"] = sessionId;
            return File(imgBytes, "image/png");
        }

        [HttpPost("tra-cuu")]
        public async Task<IActionResult> TraCuu([FromForm] string mst, [FromForm] string captcha, [FromHeader(Name = "X-Session-Id")] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || !_sessions.ContainsKey(sessionId))
            {
                return BadRequest(new { error = "Session không hợp lệ hoặc đã hết hạn." });
            }

            var cookieContainer = _sessions[sessionId];

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            client.DefaultRequestHeaders.Referrer = new Uri("https://tracuunnt.gdt.gov.vn/tcnnt/mstdn.jsp");

            var url = "https://tracuunnt.gdt.gov.vn/tcnnt/mstdn.jsp";
            var postData = new Dictionary<string, string>
            {
                { "cm", "cm" },
                { "mst", mst },
                { "fullname", "" },
                { "address", "" },
                { "cmt", "" },
                { "captcha", captcha }
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync(url, content);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var row = doc.DocumentNode.SelectSingleNode("//div[@id='resultContainer']//tr[td]");
            if (row == null)
                return BadRequest(new { error = "Không tìm thấy kết quả. Có thể captcha sai hoặc cookie mất." });

            var cells = row.SelectNodes("td").Select(td => WebUtility.HtmlDecode(td.InnerText.Trim())).ToList();

            return Ok(new CompanyInfo
            {
                Id = int.TryParse(cells.ElementAtOrDefault(0), out var id) ? id : 0,
                MaSoThue = cells.ElementAtOrDefault(1),
                TenNguoiNopThue = cells.ElementAtOrDefault(2),
                DiaChi = cells.ElementAtOrDefault(3),
                QuanLyThue = cells.ElementAtOrDefault(4),
                TrangThaiMST = cells.ElementAtOrDefault(5)
            });
        }

        [HttpPost("luu-data")]
        public async Task<IActionResult> LuuDoanhNghiep([FromBody] CompanyInfo dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.MaSoThue))
                return BadRequest(new { error = "Dữ liệu không hợp lệ" });

            if (await _db.CheckExistAsync(dto.MaSoThue))
                return BadRequest(new { error = "Mã số thuế đã tồn tại" });

            var newId = await _db.SaveDoanhNghiepAsync(dto);
            return Ok(new { success = true, id = newId });
        }

        [HttpGet("danh-sach")]
        public async Task<IActionResult> DanhSach()
        {
            var list = await _db.GetAllAsync();
            return Ok(list);
        }


    }
}
