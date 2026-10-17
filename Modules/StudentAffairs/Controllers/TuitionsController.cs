using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs.Controllers;

/// <summary>SA-02, SA-03: Học phí, thanh toán, công nợ.</summary>
[ApiController]
[Route("api/cong-tac-sinh-vien")]
[Tags("StudentAffairs")]
public class TuitionsController(ITuitionsService service) : ControllerBase
{
    private readonly ITuitionsService _service = service;

    // SA-02 Tính học phí cả học kỳ
    [HttpPost("hoc-phi/tinh/{hocKyId:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<TinhHocPhiResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TinhHocPhi(int hocKyId)
    {
        try
        {
            var result = await _service.TinhHocPhiAsync(hocKyId);
            return Ok(ApiResponse<TinhHocPhiResponse>.Ok(result, "Đã tính học phí thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Tính lại học phí cho riêng 1 sinh viên
    [HttpPost("hoc-phi/tinh/{hocKyId:int}/sinh-vien/{sinhVienId:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<TinhHocPhiResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TinhHocPhiSinhVien(int hocKyId, int sinhVienId)
    {
        try
        {
            var result = await _service.TinhHocPhiSinhVienAsync(hocKyId, sinhVienId);
            return Ok(ApiResponse<TinhHocPhiResponse>.Ok(result, "Đã tính lại học phí cho sinh viên."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Xem danh sách học phí
    [HttpGet("hoc-phi")]
    [ProducesResponseType(typeof(ApiResponse<List<HocPhiResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHocPhi(
        [FromQuery] int? sinhVienId,
        [FromQuery] int? hocKyId)
    {
        var result = await _service.GetHocPhiAsync(sinhVienId, hocKyId);
        return Ok(ApiResponse<List<HocPhiResponse>>.Ok(result));
    }

    // SA-03 Nộp học phí (từng đợt)
    [HttpPut("hoc-phi/{id:int}/thanh-toan")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<ThanhToanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ThanhToan(int id, [FromBody] ThanhToanRequest request)
    {
        try
        {
            var result = await _service.ThanhToanAsync(id, request);
            return Ok(ApiResponse<ThanhToanResponse>.Ok(result, "Đã ghi nhận thanh toán học phí."));
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

    // Xem lịch sử giao dịch của 1 bản ghi học phí
    [HttpGet("hoc-phi/{id:int}/giao-dich")]
    [ProducesResponseType(typeof(ApiResponse<List<GiaoDichHocPhiResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGiaoDich(int id)
    {
        try
        {
            var result = await _service.GetGiaoDichAsync(id);
            return Ok(ApiResponse<List<GiaoDichHocPhiResponse>>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Kiểm tra công nợ của sinh viên (dùng cho đăng ký học phần / xét tốt nghiệp)
    [HttpGet("hoc-phi/kiem-tra-no/{sinhVienId:int}")]
    [ProducesResponseType(typeof(ApiResponse<KiemTraNoHocPhiResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> KiemTraNo(int sinhVienId)
    {
        try
        {
            var result = await _service.KiemTraNoHocPhiAsync(sinhVienId);
            return Ok(ApiResponse<KiemTraNoHocPhiResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
