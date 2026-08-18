using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Application.Interfaces;
using RMS.Infrastructure.Data;

namespace RMS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;
    private readonly RmsDbContext _context;
    private User? _fallbackDevUser;
    private bool _fallbackLoaded;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, RmsDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _context = context;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <summary>
    /// DEV-ONLY. Real login (Keycloak) isn't wired on the frontend yet, so every request
    /// arrives unauthenticated. Rather than have every write fail with Forbidden, fall back
    /// to the seeded demo identity (RmsDevelopmentSeeder) - but only in Development, so this
    /// can never silently activate anywhere real auth is actually expected to be enforced.
    /// Remove this fallback once login is wired end-to-end.
    ///
    /// Honors an optional "X-Dev-User-Id" header (also Development-only, unauthenticated-only)
    /// so Feature 3 testing can simulate acting as different seeded demo users - see the
    /// frontend's "Acting as" selector in header.component.ts, which is the only thing that
    /// sends this header. Falls back to the seeded SystemAdmin when absent/invalid, same as
    /// before this existed.
    /// </summary>
    private User? FallbackDevUser
    {
        get
        {
            if (_fallbackLoaded) return _fallbackDevUser;
            _fallbackLoaded = true;
            if (_environment.IsDevelopment())
            {
                var devUserIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Dev-User-Id"].FirstOrDefault();
                if (Guid.TryParse(devUserIdHeader, out var devUserId))
                {
                    _fallbackDevUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == devUserId);
                }
                _fallbackDevUser ??= _context.Users.AsNoTracking().FirstOrDefault(u => u.Role == UserRole.SystemAdmin);
            }
            return _fallbackDevUser;
        }
    }

    public Guid? UserId
    {
        get
        {
            if (IsAuthenticated)
            {
                var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
            return FallbackDevUser?.Id;
        }
    }

    public string? FullName => IsAuthenticated ? User?.FindFirstValue(ClaimTypes.Name) : FallbackDevUser?.FullName;

    public UserRole? Role
    {
        get
        {
            if (IsAuthenticated)
            {
                var value = User?.FindFirstValue(ClaimTypes.Role);
                return Enum.TryParse<UserRole>(value, out var role) ? role : null;
            }
            return FallbackDevUser?.Role;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            if (IsAuthenticated)
            {
                var value = User?.FindFirstValue("companyId");
                return Guid.TryParse(value, out var id) ? id : null;
            }
            return FallbackDevUser?.CompanyId;
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(UserRole role) => Role == role;
}
