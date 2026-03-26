using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;

namespace BankMaskingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly SecurityModules _security;

        public CustomerController(IConfiguration config)
        {
            _security = new SecurityModules(config);
        }

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
    }
}