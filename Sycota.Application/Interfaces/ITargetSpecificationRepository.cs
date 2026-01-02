using Sycota.Application.Interfaces;
using Sycota.Domain.Enums;
using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface ITargetSpecificationRepository
{
    Task<TargetSpecification?> GetByIdAsync(int id);
    Task<TargetSpecification?> GetByWeaponTypeAsync(ISSFWeaponType weaponType);
    Task<IEnumerable<TargetSpecification>> GetAllAsync();
    Task AddAsync(TargetSpecification spec);
    Task UpdateAsync(TargetSpecification spec);
    Task DeleteAsync(TargetSpecification spec);
    Task DeleteByIdAsync(int id);
}