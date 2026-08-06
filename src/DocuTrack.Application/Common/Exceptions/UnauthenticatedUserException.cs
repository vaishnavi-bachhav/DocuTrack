namespace DocuTrack.Application.Common.Exceptions
{
    public sealed class UnauthenticatedUserException : Exception
    {
        public UnauthenticatedUserException()
            : base("An authenticated user is required.")
        {
        }
    }
}
