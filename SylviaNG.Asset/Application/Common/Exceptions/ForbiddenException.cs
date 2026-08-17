namespace SylviaNG.Assets.Application.Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "You are not authorized to perform this action.") : base(message)
        {
        }
    }
}
