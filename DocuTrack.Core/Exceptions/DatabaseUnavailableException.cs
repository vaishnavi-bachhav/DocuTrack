namespace DocuTrack.Core.Exceptions
{
    public sealed class DatabaseUnavailableException : Exception
    {
        public DatabaseUnavailableException() : base("Database is currently unavailable.") { }

        public DatabaseUnavailableException(Exception innerException) : base("The database is temporarily unavailable.",
            innerException)
        {
        }
    }
}
