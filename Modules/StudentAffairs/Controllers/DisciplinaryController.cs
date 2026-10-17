using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs.Controllers;

/// <summary>SA-06: Khen thưởng / kỷ luật.</summary>
[ApiController]
[Route("api/cong-tac-sinh-vien")]
[Tags("StudentAffairs")]
public class DisciplinaryController(IDisciplinaryService service) : ControllerBase
{
    private readonly IDisciplinaryService _service = service;

    [HttpPost("khen-thuong-ky-luat")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<KhenThuongKyLuatResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateKhenThuongKyLuat([FromBody] KhenThuongKyLuatRequest request)
    {
        try
        {
            var result = await _service.CreateKhenThuongKyLuatAsync(request);
            return Ok(ApiResponse<KhenThuongKyLuatResponse>.Ok(result, "Tạo quyết định thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
