namespace RMS.Application.Features.RequisitionCategories.DTOs;

/// <summary>Price is Feature 4's "price integrated type-wise" - nullable, informational only.</summary>
public record CategoryItemInput(string Name, bool IsActive, decimal? Price = null);
