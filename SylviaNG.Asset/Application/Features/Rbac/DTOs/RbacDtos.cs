using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.DTOs;

public record RoleSummaryDto(UserRole Role, string RoleLabel, int ActiveUserCount, int TotalUserCount);

public record PermissionCellDto(PermissionModule Module, PermissionAction Action, bool IsAllowed);

public record PermissionMatrixDto(UserRole Role, List<PermissionCellDto> Permissions);
