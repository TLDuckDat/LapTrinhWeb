using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs.Controllers;

/// <summary>SA-04: Miễn giảm học phí.</summary>
[ApiController]
[Route("api/cong-tac-sinh-vien")]
[Tags("StudentAffairs")]
public class TuitionDiscountsController(ITuitionDiscountsService service) : ControllerBase
{
    private readonly ITuitionDiscountsService _service = service;

    // Xem danh sách miễn giảm
    [HttpGet("mien-giam")]
    [ProducesResponseType(typeof(ApiResponse<List<MienGiamResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMienGiam(
        [FromQuery] int? sinhVienId,
        [FromQuery] int? hocKyId)
    {
        var result = await _service.GetMienGiamAsync(sinhVienId, hocKyId);
        return Ok(ApiResponse<List<MienGiamResponse>>.Ok(result));
    }

    // Tạo miễn giảm
    [HttpPost("mien-giam")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<MienGiamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMienGiam([FromBody] MienGiamRequest request)
    {
        try
        {
            var result = await _service.CreateMienGiamAsync(request);
            return Ok(ApiResponse<MienGiamResponse>.Ok(result, "Tạo miễn giảm học phí thành công."));
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

    // Sửa tỷ lệ / lý do miễn giảm
    [HttpPut("mien-giam/{id:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<MienGiamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuaMienGiam(int id, [FromBody] SuaMienGiamRequest request)
    {
        try
        {
            var result = await _service.SuaMienGiamAsync(id, request);
            return Ok(ApiResponse<MienGiamResponse>.Ok(result, "Đã cập nhật miễn giảm học phí."));
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

    // Hủy hồ sơ miễn giảm
    [HttpDelete("mien-giam/{id:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<MienGiamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> XoaMienGiam(int id)
    {
        try
        {
            var result = await _service.XoaMienGiamAsync(id);
            return Ok(ApiResponse<MienGiamResponse>.Ok(result, "Đã hủy hồ sơ miễn giảm."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
