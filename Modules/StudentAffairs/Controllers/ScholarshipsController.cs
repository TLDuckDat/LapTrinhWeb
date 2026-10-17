using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs.Controllers;

/// <summary>SA-05: Học bổng.</summary>
[ApiController]
[Route("api/cong-tac-sinh-vien")]
[Tags("StudentAffairs")]
public class ScholarshipsController(IScholarshipsService service) : ControllerBase
{
    private readonly IScholarshipsService _service = service;

    // Xét học bổng
    [HttpPost("hoc-bong/xet/{hocKyId:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<XetHocBongResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> XetHocBong(int hocKyId)
    {
        try
        {
            var result = await _service.XetHocBongAsync(hocKyId);
            return Ok(ApiResponse<XetHocBongResponse>.Ok(result, "Xét học bổng thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Tra cứu danh sách đã nhận học bổng (sinh viên và cán bộ đều xem được)
    [HttpGet("hoc-bong")]
    [ProducesResponseType(typeof(ApiResponse<List<HocBongResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHocBong(
        [FromQuery] int? hocKyId,
        [FromQuery] int? sinhVienId)
    {
        var result = await _service.GetHocBongAsync(hocKyId, sinhVienId);
        return Ok(ApiResponse<List<HocBongResponse>>.Ok(result));
    }
}
