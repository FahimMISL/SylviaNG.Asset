namespace RMS.Domain.Common;

/// <summary>Thrown when a domain invariant / business rule is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
