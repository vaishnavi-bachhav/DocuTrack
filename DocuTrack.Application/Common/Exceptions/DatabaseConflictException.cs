namespace DocuTrack.Application.Common.Exceptions
{
    public sealed class DatabaseConflictException : Exception
    {
        public DatabaseConflictException(string message) : base(message)
        {
        }

        public DatabaseConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
