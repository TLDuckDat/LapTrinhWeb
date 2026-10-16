using Microsoft.AspNetCore.Mvc;
using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Services;

namespace StudentManagement.Modules.Students.Controllers;

[ApiController]
[Route("api/lop-hanh-chinh")]
[Tags("Students")]
public class LopHanhChinhController(ILopHanhChinhService service) : ControllerBase
{
    private readonly ILopHanhChinhService _service = service;

    // STU-04: Danh sách lớp hành chính (phân trang, lọc theo ngành / khóa)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LopHanhChinhDto>>>> GetPaged([FromQuery] LopHanhChinhFilterQuery query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<LopHanhChinhDto>>.Ok(result));
    }

    // STU-04: Chi tiết lớp hành chính kèm danh sách sinh viên bên trong
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<LopHanhChinhDetailDto>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? NotFound(ApiResponse<LopHanhChinhDetailDto>.Fail("Không tìm thấy lớp hành chính."))
            : Ok(ApiResponse<LopHanhChinhDetailDto>.Ok(result));
    }

    // STU-04: Thêm mới lớp hành chính
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LopHanhChinhDto>>> Create([FromBody] LopHanhChinhRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<LopHanhChinhDto>.Ok(result, "Đã thêm lớp hành chính thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LopHanhChinhDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<LopHanhChinhDto>.Fail(ex.Message));
        }
    }

    // STU-04: Cập nhật lớp hành chính
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<LopHanhChinhDto>>> Update(int id, [FromBody] LopHanhChinhRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<LopHanhChinhDto>.Fail("Không tìm thấy lớp hành chính."))
                : Ok(ApiResponse<LopHanhChinhDto>.Ok(result, "Đã cập nhật thông tin lớp hành chính."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LopHanhChinhDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<LopHanhChinhDto>.Fail(ex.Message));
        }
    }

    // STU-04: Xóa lớp hành chính
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            return success
                ? Ok(ApiResponse<object>.Ok(null, "Đã xóa lớp hành chính."))
                : NotFound(ApiResponse<object>.Fail("Không tìm thấy lớp hành chính."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // STU-05: Phân công Cố vấn học tập cho lớp
    [HttpPut("{id:int}/phan-co-van")]
    public async Task<ActionResult<ApiResponse<LopHanhChinhDto>>> PhanCoVan(int id, [FromBody] PhanCoVanRequest request)
    {
        try
        {
            var result = await _service.PhanCoVanAsync(id, request);
            return result is null
                ? NotFound(ApiResponse<LopHanhChinhDto>.Fail("Không tìm thấy lớp hành chính."))
                : Ok(ApiResponse<LopHanhChinhDto>.Ok(result, "Đã phân công cố vấn học tập thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LopHanhChinhDto>.Fail(ex.Message));
        }
    }

    // STU-04: Phân sinh viên hàng loạt vào lớp hành chính
    [HttpPut("{id:int}/phan-lop")]
    public async Task<ActionResult<ApiResponse<object>>> PhanLop(int id, [FromBody] PhanLopRequest request)
    {
        try
        {
            var updatedRows = await _service.PhanLopAsync(id, request);
            return Ok(ApiResponse<object>.Ok(new { SoLuongCapNhat = updatedRows }, $"Đã phân {updatedRows} sinh viên vào lớp thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
