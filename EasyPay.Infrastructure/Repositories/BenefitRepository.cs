using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class BenefitRepository : IBenefitRepository
{
    private readonly EasyPayDbContext _context;

    public BenefitRepository(EasyPayDbContext context) { _context = context; }

    public async Task<IEnumerable<Benefit>> GetAllAsync() =>
        await _context.Benefits.AsNoTracking().OrderBy(b => b.BenefitName).ToListAsync();

    public async Task<Benefit?> GetByIdAsync(int id) =>
        await _context.Benefits.AsNoTracking().FirstOrDefaultAsync(b => b.BenefitId == id);

    public async Task<Benefit> AddAsync(Benefit benefit)
    {
        _context.Benefits.Add(benefit);
        await _context.SaveChangesAsync();
        return benefit;
    }

    public async Task<IEnumerable<EmployeeBenefit>> GetEmployeeBenefitsAsync(int employeeId) =>
        await _context.EmployeeBenefits
            .Include(eb => eb.Employee).Include(eb => eb.Benefit)
            .AsNoTracking().Where(eb => eb.EmployeeId == employeeId).ToListAsync();

    public async Task<EmployeeBenefit> AssignBenefitAsync(EmployeeBenefit employeeBenefit)
    {
        _context.EmployeeBenefits.Add(employeeBenefit);
        await _context.SaveChangesAsync();
        return employeeBenefit;
    }

    public async Task<bool> RemoveBenefitAsync(int employeeBenefitId)
    {
        var record = await _context.EmployeeBenefits.FindAsync(employeeBenefitId);
        if (record == null) return false;
        _context.EmployeeBenefits.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BenefitAssignedAsync(int employeeId, int benefitId) =>
        await _context.EmployeeBenefits.AnyAsync(eb => eb.EmployeeId == employeeId && eb.BenefitId == benefitId);

    /// <summary>Returns total allowance and total deduction from all assigned benefits</summary>
    public async Task<(decimal TotalAllowance, decimal TotalDeduction)> GetBenefitTotalsAsync(int employeeId)
    {
        var records = await _context.EmployeeBenefits
            .Include(eb => eb.Benefit)
            .AsNoTracking()
            .Where(eb => eb.EmployeeId == employeeId)
            .ToListAsync();

        var allowance = records
            .Where(eb => eb.Benefit.BenefitType == BenefitType.Allowance)
            .Sum(eb => eb.Amount);

        var deduction = records
            .Where(eb => eb.Benefit.BenefitType == BenefitType.Deduction)
            .Sum(eb => eb.Amount);

        return (allowance, deduction);
    }
}
