using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Services;

namespace StudentManagement.Modules.Students.Controllers;

[ApiController]
[Route("api/sinh-vien")]
[Tags("Students")]
public class SinhVienController(ISinhVienService service) : ControllerBase
{
    private readonly ISinhVienService _service = service;

    // STU-01, STU-03: Danh sách sinh viên (phân trang, lọc đa tiêu chí)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SinhVienDto>>>> GetPaged([FromQuery] SinhVienFilterQuery query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<SinhVienDto>>.Ok(result));
    }

    // STU-02: Xem chi tiết hồ sơ sinh viên
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
            : Ok(ApiResponse<SinhVienDetailDto>.Ok(result));
    }

    // STU-01: Thêm sinh viên
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> Create([FromBody] CreateSinhVienRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<SinhVienDetailDto>.Ok(result, "Đã tạo hồ sơ sinh viên thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-01: Cập nhật thông tin sinh viên
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> Update(int id, [FromBody] UpdateSinhVienRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã cập nhật hồ sơ sinh viên."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-01: Xóa sinh viên
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            return success
                ? Ok(ApiResponse<object>.Ok(null, "Đã xóa sinh viên."))
                : NotFound(ApiResponse<object>.Fail("Không tìm thấy sinh viên."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // STU-04: Chuyển lớp hành chính cho sinh viên
    [HttpPut("{id:int}/chuyen-lop")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> ChuyenLop(int id, [FromBody] ChuyenLopHanhChinhRequest request)
    {
        try
        {
            var result = await _service.ChuyenLopAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã chuyển lớp cho sinh viên thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-06: Đổi trạng thái học tập
    [HttpPut("{id:int}/trang-thai")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        try
        {
            var result = await _service.ChangeStatusAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã cập nhật trạng thái học tập của sinh viên."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-07: Tiếp nhận đơn bảo lưu
    [HttpPost("{id:int}/bao-luu")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> BaoLuu(int id, [FromBody] BaoLuuRequest request)
    {
        try
        {
            var result = await _service.BaoLuuAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã tiếp nhận đơn bảo lưu của sinh viên."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-07: Ra quyết định thôi học
    [HttpPost("{id:int}/thoi-hoc")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> ThoiHoc(int id, [FromBody] ThoiHocRequest request)
    {
        try
        {
            var result = await _service.ThoiHocAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã ghi nhận quyết định thôi học của sinh viên."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-07: Tiếp nhận quay lại học sau bảo lưu
    [HttpPost("{id:int}/quay-lai-hoc")]
    public async Task<ActionResult<ApiResponse<SinhVienDetailDto>>> QuayLaiHoc(int id, [FromBody] QuayLaiHocRequest request)
    {
        try
        {
            var result = await _service.QuayLaiHocAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<SinhVienDetailDto>.Fail("Không tìm thấy sinh viên."))
                : Ok(ApiResponse<SinhVienDetailDto>.Ok(result, "Đã tiếp nhận sinh viên quay trở lại học tập."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SinhVienDetailDto>.Fail(ex.Message));
        }
    }

    // STU-07: Xem lịch sử biến động trạng thái học tập của sinh viên
    [HttpGet("{id:int}/lich-su-hoc-tap")]
    public async Task<ActionResult<ApiResponse<List<LichSuHocTapDto>>>> GetLichSuHocTap(int id)
    {
        try
        {
            var result = await _service.GetLichSuHocTapAsync(id);
            return Ok(ApiResponse<List<LichSuHocTapDto>>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<LichSuHocTapDto>>.Fail(ex.Message));
        }
    }
}
