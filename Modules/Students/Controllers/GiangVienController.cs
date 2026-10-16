using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Services;

namespace StudentManagement.Modules.Students.Controllers;

[ApiController]
[Route("api/giang-vien")]
[Tags("Students")]
public class GiangVienController(IGiangVienService service) : ControllerBase
{
    private readonly IGiangVienService _service = service;

    // STU-05: Danh sách giảng viên (phân trang, lọc theo bộ môn / khoa)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GiangVienDto>>>> GetPaged([FromQuery] GiangVienFilterQuery query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<GiangVienDto>>.Ok(result));
    }

    // STU-05: Chi tiết giảng viên
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GiangVienDto>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? NotFound(ApiResponse<GiangVienDto>.Fail("Không tìm thấy giảng viên."))
            : Ok(ApiResponse<GiangVienDto>.Ok(result));
    }

    // STU-05: Thêm mới giảng viên
    [HttpPost]
    public async Task<ActionResult<ApiResponse<GiangVienDto>>> Create([FromBody] GiangVienRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<GiangVienDto>.Ok(result, "Đã thêm giảng viên thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GiangVienDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<GiangVienDto>.Fail(ex.Message));
        }
    }

    // STU-05: Cập nhật giảng viên
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<GiangVienDto>>> Update(int id, [FromBody] GiangVienRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<GiangVienDto>.Fail("Không tìm thấy giảng viên."))
                : Ok(ApiResponse<GiangVienDto>.Ok(result, "Đã cập nhật thông tin giảng viên."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GiangVienDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<GiangVienDto>.Fail(ex.Message));
        }
    }

    // STU-05: Xóa giảng viên
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            return success
                ? Ok(ApiResponse<object>.Ok(null, "Đã xóa giảng viên."))
                : NotFound(ApiResponse<object>.Fail("Không tìm thấy giảng viên."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
