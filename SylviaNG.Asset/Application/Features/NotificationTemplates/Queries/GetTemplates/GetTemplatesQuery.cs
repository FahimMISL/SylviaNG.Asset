using MediatR;
using RMS.Application.Features.NotificationTemplates.DTOs;

namespace RMS.Application.Features.NotificationTemplates.Queries.GetTemplates;

public record GetTemplatesQuery : IRequest<List<NotificationTemplateDto>>;
