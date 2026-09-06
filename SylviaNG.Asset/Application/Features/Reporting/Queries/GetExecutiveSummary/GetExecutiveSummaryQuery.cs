using MediatR;
using RMS.Application.Features.Reporting.DTOs;

namespace RMS.Application.Features.Reporting.Queries.GetExecutiveSummary;

/// <summary>US-025. No filters - a full, unfiltered snapshot of the company's requisitions.</summary>
public record GetExecutiveSummaryQuery : IRequest<ExecutiveSummaryDto>;
