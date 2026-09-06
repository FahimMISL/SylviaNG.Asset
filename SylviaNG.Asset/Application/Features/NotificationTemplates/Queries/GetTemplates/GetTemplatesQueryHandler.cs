using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.NotificationTemplates.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.Queries.GetTemplates;

public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTemplatesQueryHandler(INotificationTemplateRepository templateRepository, ICurrentUserService currentUser)
    {
        _templateRepository = templateRepository;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationTemplateDto>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var customized = (await _templateRepository.GetAllAsync(companyId, cancellationToken))
            .ToDictionary(t => t.EventType);

        return NotificationEventTypeCatalog.AllTypes
            .Select(eventType => customized.TryGetValue(eventType, out var custom)
                ? NotificationTemplateDto.FromCustom(custom)
                : NotificationTemplateDto.FromDefault(eventType))
            .ToList();
    }
}
