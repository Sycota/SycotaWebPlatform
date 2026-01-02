using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class TargetSpecificationRepository : ITargetSpecificationRepository
{
    private readonly ApplicationDbContext _context;

    public TargetSpecificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TargetSpecification?> GetByIdAsync(int id)
    {
        return await _context.Set<TargetSpecification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TargetSpecification?> GetByWeaponTypeAsync(ISSFWeaponType weaponType)
    {
        return await _context.Set<TargetSpecification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.WeaponType == weaponType);
    }

    public async Task<IEnumerable<TargetSpecification>> GetAllAsync()
    {
        return await _context.Set<TargetSpecification>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(TargetSpecification spec)
    {
        await _context.Set<TargetSpecification>().AddAsync(spec);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TargetSpecification spec)
    {
        _context.Set<TargetSpecification>().Update(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TargetSpecification spec)
    {
        _context.Set<TargetSpecification>().Remove(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(int id)
    {
        var entity = await _context.Set<TargetSpecification>().FindAsync(id);
        if (entity is null) return;
        _context.Set<TargetSpecification>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}