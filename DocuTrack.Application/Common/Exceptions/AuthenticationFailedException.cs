namespace DocuTrack.Application.Common.Exceptions
{
    public sealed class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException()
        : base("The email or password is incorrect.")
        {
        }
    }
}
