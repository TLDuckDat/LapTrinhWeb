using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs.Controllers;

/// <summary>SA-01: Điểm rèn luyện.</summary>
[ApiController]
[Route("api/cong-tac-sinh-vien")]
[Tags("StudentAffairs")]
public class ConductScoresController(IConductScoresService service) : ControllerBase
{
    private readonly IConductScoresService _service = service;

    // Xem danh sách điểm rèn luyện
    [HttpGet("diem-ren-luyen")]
    [ProducesResponseType(typeof(ApiResponse<List<DiemRenLuyenResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiemRenLuyen(
        [FromQuery] int? sinhVienId,
        [FromQuery] int? hocKyId)
    {
        var result = await _service.GetDiemRenLuyenAsync(sinhVienId, hocKyId);
        return Ok(ApiResponse<List<DiemRenLuyenResponse>>.Ok(result));
    }

    // Nhập điểm rèn luyện (chỉ cán bộ / giảng viên)
    [HttpPost("diem-ren-luyen")]
    [Authorize(Roles = "Admin,PhongDaoTao,GiangVien")]
    [ProducesResponseType(typeof(ApiResponse<DiemRenLuyenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> NhapDiemRenLuyen([FromBody] DiemRenLuyenRequest request)
    {
        try
        {
            bool laAdmin = User.IsInRole("Admin");
            var result = await _service.NhapDiemRenLuyenAsync(request, laAdmin);
            return Ok(ApiResponse<DiemRenLuyenResponse>.Ok(result, "Đã nhập điểm rèn luyện."));
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

    // Sửa điểm rèn luyện (bắt buộc lý do, tự lưu người sửa / ngày sửa)
    [HttpPut("diem-ren-luyen/{id:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao,GiangVien")]
    [ProducesResponseType(typeof(ApiResponse<DiemRenLuyenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuaDiemRenLuyen(int id, [FromBody] SuaDiemRenLuyenRequest request)
    {
        try
        {
            // Lấy thông tin người sửa từ token đăng nhập
            int? nguoiSuaId = null;
            if (int.TryParse(User.FindFirst("sub")?.Value, out int taiKhoanId))
                nguoiSuaId = taiKhoanId;

            string? tenNguoiSua = User.FindFirst("name")?.Value;
            bool laAdmin = User.IsInRole("Admin");

            var result = await _service.SuaDiemRenLuyenAsync(id, request, nguoiSuaId, tenNguoiSua, laAdmin);
            return Ok(ApiResponse<DiemRenLuyenResponse>.Ok(result, "Đã sửa điểm rèn luyện."));
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

    // Xem lịch sử sửa điểm
    [HttpGet("diem-ren-luyen/{id:int}/lich-su-sua")]
    [ProducesResponseType(typeof(ApiResponse<List<LichSuSuaDiemRenLuyenResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLichSuSua(int id)
    {
        try
        {
            var result = await _service.GetLichSuSuaAsync(id);
            return Ok(ApiResponse<List<LichSuSuaDiemRenLuyenResponse>>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Khóa sổ điểm rèn luyện cả học kỳ (Phòng CTSV duyệt)
    [HttpPost("diem-ren-luyen/khoa-so/{hocKyId:int}")]
    [Authorize(Roles = "Admin,PhongDaoTao")]
    [ProducesResponseType(typeof(ApiResponse<KhoaSoDiemRenLuyenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> KhoaSo(int hocKyId)
    {
        try
        {
            var result = await _service.KhoaSoAsync(hocKyId);
            return Ok(ApiResponse<KhoaSoDiemRenLuyenResponse>.Ok(result, "Đã khóa sổ điểm rèn luyện."));
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

    // Mở khóa sổ (chỉ Admin)
    [HttpPost("diem-ren-luyen/mo-khoa-so/{hocKyId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<KhoaSoDiemRenLuyenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoKhoaSo(int hocKyId)
    {
        try
        {
            var result = await _service.MoKhoaSoAsync(hocKyId);
            return Ok(ApiResponse<KhoaSoDiemRenLuyenResponse>.Ok(result, "Đã mở khóa sổ điểm rèn luyện."));
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
