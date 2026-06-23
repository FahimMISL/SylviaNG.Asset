namespace SylviaNG.Assets.Application.Common.Exceptions
{
    public class DuplicateException : Exception
    {
        public DuplicateException(string message) : base(message)
        {
        }

        public DuplicateException(string name, string key, string value)
            : base($"Entity \"{name}\" with {key} \"{value}\" already exists.")
        {
        }
    }
}
