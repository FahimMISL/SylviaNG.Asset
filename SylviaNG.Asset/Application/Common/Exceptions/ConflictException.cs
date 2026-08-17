namespace SylviaNG.Assets.Application.Common.Exceptions
{
    /// <summary>Thrown when a request is well-formed but violates a state/business rule (e.g. delete blocked by existing references).</summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
