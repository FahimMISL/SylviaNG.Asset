namespace RMS.Api.Controllers.Requests;

public record CreateDelegationRequestBody(Guid DelegateUserId, DateOnly StartDate, DateOnly EndDate, string Reason, Guid? OnBehalfOfUserId);

public record RevokeDelegationRequestBody(string Reason);
