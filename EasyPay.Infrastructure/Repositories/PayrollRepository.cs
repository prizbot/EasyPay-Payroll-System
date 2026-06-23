using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class PayrollRepository : IPayrollRepository
{
    private readonly EasyPayDbContext _context;

    public PayrollRepository(EasyPayDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payroll>> GetAllAsync()
    {
        return await _context.Payrolls
            .Include(p => p.Employee)
            .AsNoTracking()
            .OrderByDescending(p => p.PayYear)
            .ThenByDescending(p => p.PayMonth)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payroll>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Payrolls
            .Include(p => p.Employee)
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PayYear)
            .ThenByDescending(p => p.PayMonth)
            .ToListAsync();
    }

    public async Task<Payroll?> GetByIdAsync(int id)
    {
        return await _context.Payrolls
            .Include(p => p.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PayrollId == id);
    }

    public async Task<Payroll?> GetByEmployeeMonthYearAsync(int employeeId, int month, int year)
    {
        return await _context.Payrolls
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId
                                   && p.PayMonth == month
                                   && p.PayYear == year);
    }

    public async Task<Payroll> AddAsync(Payroll payroll)
    {
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();
        return payroll;
    }

    public async Task<decimal> GetTotalPayrollForMonthAsync(int month, int year)
    {
        return await _context.Payrolls
            .Where(p => p.PayMonth == month && p.PayYear == year)
            .SumAsync(p => (decimal?)p.NetSalary) ?? 0m;
    }

    public async Task<IEnumerable<PayStub>> GetPayStubsByEmployeeIdAsync(int employeeId)
    {
        return await _context.PayStubs
            .Include(ps => ps.Payroll)
                .ThenInclude(p => p.Employee)
            .AsNoTracking()
            .Where(ps => ps.Payroll.EmployeeId == employeeId)
            .OrderByDescending(ps => ps.GeneratedDate)
            .ToListAsync();
    }

    public async Task AddPayStubAsync(PayStub payStub)
    {
        _context.PayStubs.Add(payStub);
        await _context.SaveChangesAsync();
    }
}
