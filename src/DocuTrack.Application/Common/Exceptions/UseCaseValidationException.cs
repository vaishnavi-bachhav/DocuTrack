namespace DocuTrack.Application.Common.Exceptions
{
    public sealed class UseCaseValidationException : Exception
    {
        public UseCaseValidationException(string message) : base(message)
        {
        }
    }
}
