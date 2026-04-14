using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;

namespace BankMaskingAPI.Controllers
{
    public sealed class OperatorLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string ClientId { get; set; } = "";
    }

    public sealed class CskhUpdateRequestApiModel
    {
        public long CustomerId { get; set; }
        public string RequestReason { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string NgaySinh { get; set; } = "";
        public int? GioiTinh { get; set; }
        public string QuocTich { get; set; } = "";
        public string DiaChiNha { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string Email { get; set; } = "";
        public string SoCCCD { get; set; } = "";
    }

    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly SecurityModules _security;
        private readonly JwtTokenService _tokenService;

        public CustomerController(SecurityModules security, JwtTokenService tokenService)
        {
            _security = security;
            _tokenService = tokenService;
        }

        [Authorize(Roles = "cskh")]
        [HttpGet("cskh/search")]
        public IActionResult SearchCustomerCSKH([FromQuery] int type, [FromQuery] string keyword)
        {
            try
            {
                if (!IsClientBoundSessionValid())
                {
                    return Unauthorized(new { message = "Phiên truy cập không hợp lệ." });
                }

                var result = _security.SearchCustomerCskh(type, keyword);
                if (!result.Found)
                    return NotFound(new { message = result.DisplayText });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        [Authorize(Roles = "dev")]
        [HttpGet("dev/export")]
        public IActionResult ExportDev()
        {
            try
            {
                var result = _security.ExportDevSample();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("auth/login")]
        public IActionResult Login([FromBody] OperatorLoginRequest request)
        {
            try
            {
                var result = _security.ValidateOperatorLogin(request?.Username ?? "", request?.Password ?? "");
                if (!result.Success)
                {
                    return Unauthorized(result);
                }

                string normalizedUser = (request?.Username ?? string.Empty).Trim().ToLowerInvariant();
                string normalizedClientId = (request?.ClientId ?? string.Empty).Trim();
                if (normalizedClientId.Length > 128)
                {
                    normalizedClientId = normalizedClientId[..128];
                }

                var tokenPayload = _tokenService.CreateToken(normalizedUser, result.Role, result.CustomerId, normalizedClientId);
                result.Token = tokenPayload.Token;
                result.ExpiresAtUtc = tokenPayload.ExpiresAtUtc;

                ApplySensitiveResponseHeaders();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        [Authorize(Roles = "kh")]
        [HttpGet("me/profile")]
        public IActionResult MyProfile()
        {
            try
            {
                string? customerIdText = User.FindFirstValue("customer_id");
                if (!long.TryParse(customerIdText, out long customerId) || customerId <= 0)
                {
                    return Unauthorized(new { message = "Token không hợp lệ." });
                }

                string tokenClientId = (User.FindFirstValue("client_id") ?? string.Empty).Trim();
                string requestClientId = (Request.Headers["X-Client-Id"].ToString() ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(tokenClientId)
                    && !string.Equals(tokenClientId, requestClientId, StringComparison.Ordinal))
                {
                    return Unauthorized(new { message = "Phiên truy cập không hợp lệ." });
                }

                var profile = _security.GetCustomerSelfProfile(customerId);
                if (!profile.Found)
                {
                    return NotFound(new { message = profile.Message });
                }

                ApplySensitiveResponseHeaders();
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        [Authorize(Roles = "cskh")]
        [HttpPost("cskh/update-request")]
        public IActionResult SubmitCustomerUpdateRequest([FromBody] CskhUpdateRequestApiModel request)
        {
            try
            {
                if (!IsClientBoundSessionValid())
                {
                    return Unauthorized(new { message = "Phiên truy cập không hợp lệ." });
                }

                string? actorIdText = User.FindFirstValue("customer_id");
                if (!long.TryParse(actorIdText, out long actorId) || actorId <= 0)
                {
                    return Unauthorized(new { message = "Token không hợp lệ." });
                }

                var model = new CskhCustomerInfoUpdateRequest
                {
                    CustomerId = request?.CustomerId ?? 0,
                    RequestReason = request?.RequestReason ?? string.Empty,
                    HoTen = request?.HoTen ?? string.Empty,
                    NgaySinh = request?.NgaySinh ?? string.Empty,
                    GioiTinh = request?.GioiTinh,
                    QuocTich = request?.QuocTich ?? string.Empty,
                    DiaChiNha = request?.DiaChiNha ?? string.Empty,
                    SoDienThoai = request?.SoDienThoai ?? string.Empty,
                    Email = request?.Email ?? string.Empty,
                    SoCCCD = request?.SoCCCD ?? string.Empty
                };

                var result = _security.SubmitCskhUpdateRequest(actorId, model);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                ApplySensitiveResponseHeaders();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        [Authorize(Roles = "dev")]
        [HttpGet("manage/list")]
        public IActionResult ManageList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = _security.GetCustomersForManagement(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }


        // TODO: create
        [Authorize(Roles = "dev")]
        [HttpPost("manage/create")]
        public IActionResult ManageCreate([FromBody] CustomerManageUpsertRequest request)
        {
            try
            {
                var result = _security.CreateCustomer(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        // TODO: update 
        [Authorize(Roles = "dev")]
        [HttpPut("manage/{customerId:long}")]
        public IActionResult ManageUpdate(long customerId, [FromBody] CustomerManageUpsertRequest request)
        {
            try
            {
                var result = _security.UpdateCustomer(customerId, request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        // TODO: delete
        [Authorize(Roles = "dev")]
        [HttpDelete("manage/{customerId:long}")]
        public IActionResult ManageDelete(long customerId)
        {
            try
            {
                var result = _security.DeleteCustomer(customerId);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        private void ApplySensitiveResponseHeaders()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
        }

        private bool IsClientBoundSessionValid()
        {
            string tokenClientId = (User.FindFirstValue("client_id") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(tokenClientId))
            {
                return true;
            }

            string requestClientId = (Request.Headers["X-Client-Id"].ToString() ?? string.Empty).Trim();
            return string.Equals(tokenClientId, requestClientId, StringComparison.Ordinal);
        }
    }
}