using FluentValidation;
using FluentValidation.Results;

namespace RMS.Application.Features.Approvals.Services;

/// <summary>Per spec section 12 "never rely only on Angular validation": every approval action's
/// comment is validated MinimumLength(10) in FluentValidation AND, redundantly, re-checked here in
/// the handler.</summary>
public static class CommentValidation
{
    public const int MinimumLength = 10;

    public static void EnsureValid(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < MinimumLength)
        {
            throw new ValidationException(new List<ValidationFailure>
            {
                new("Comment", $"Comment must be at least {MinimumLength} characters."),
            });
        }
    }
}
