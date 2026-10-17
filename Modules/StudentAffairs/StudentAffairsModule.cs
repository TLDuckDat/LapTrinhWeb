using StudentManagement.Modules.StudentAffairs.Services;

namespace StudentManagement.Modules.StudentAffairs;

public static class StudentAffairsModule
{
    /// <summary>Đăng ký service và cấu hình của module StudentAffairs.</summary>
    public static IServiceCollection AddStudentAffairsModule(this IServiceCollection services)
    {
        // Thông số nghiệp vụ (đơn giá tín chỉ, điều kiện học bổng, cổng nhập điểm...)
        // được đọc từ mục "StudentAffairs" trong appsettings.json
        services.AddOptions<StudentAffairsSettings>()
            .BindConfiguration("StudentAffairs");

        // Mỗi service phụ trách 1 nhóm nghiệp vụ (Single Responsibility)
        services.AddScoped<IConductScoresService, ConductScoresService>();
        services.AddScoped<ITuitionsService, TuitionsService>();
        services.AddScoped<ITuitionDiscountsService, TuitionDiscountsService>();
        services.AddScoped<IScholarshipsService, ScholarshipsService>();
        services.AddScoped<IDisciplinaryService, DisciplinaryService>();
        return services;
    }
}
