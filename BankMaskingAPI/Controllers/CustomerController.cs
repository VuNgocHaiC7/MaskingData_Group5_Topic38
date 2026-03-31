using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BankMaskingAPI.Controllers
{
    public sealed class OperatorLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
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
                var tokenPayload = _tokenService.CreateToken(normalizedUser, result.Role);
                result.Token = tokenPayload.Token;
                result.ExpiresAtUtc = tokenPayload.ExpiresAtUtc;

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
    }
}