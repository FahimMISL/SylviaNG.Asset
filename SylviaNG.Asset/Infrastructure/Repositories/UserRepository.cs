using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RmsDbContext _context;

    public UserRepository(RmsDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<List<User>> GetActiveByRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default) =>
        _context.Users
            .Where(u => u.CompanyId == companyId && u.Role == role && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

    public Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);

    public Task<List<User>> GetAllAsync(Guid companyId, UserRole? role, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.CompanyId == companyId);
        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        return query.OrderBy(u => u.FullName).ToListAsync(cancellationToken);
    }
}
