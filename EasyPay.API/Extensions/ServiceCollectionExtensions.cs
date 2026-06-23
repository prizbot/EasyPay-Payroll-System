using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Data;
using EasyPay.Infrastructure.Repositories;
using EasyPay.Infrastructure.Services;

namespace EasyPay.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<EasyPayDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)));
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository,      EmployeeRepository>();
        services.AddScoped<IUserAccountRepository,   UserAccountRepository>();
        services.AddScoped<IAttendanceRepository,    AttendanceRepository>();
        services.AddScoped<ILeaveRepository,         LeaveRepository>();
        services.AddScoped<IPayrollRepository,       PayrollRepository>();
        services.AddScoped<IBenefitRepository,       BenefitRepository>();
        services.AddScoped<IAuditRepository,         AuditRepository>();
        services.AddScoped<INotificationRepository,  NotificationRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtService,           JwtService>();
        services.AddScoped<IAuthService,          AuthService>();
        services.AddScoped<IEmployeeService,      EmployeeService>();
        services.AddScoped<IAttendanceService,    AttendanceService>();
        services.AddScoped<ILeaveService,         LeaveService>();
        services.AddScoped<IPayrollService,       PayrollService>();
        services.AddScoped<IBenefitService,       BenefitService>();
        services.AddScoped<IDashboardService,     DashboardService>();
        services.AddScoped<IAuditService,         AuditService>();
        services.AddScoped<INotificationService,  NotificationService>();
        return services;
    }
}
