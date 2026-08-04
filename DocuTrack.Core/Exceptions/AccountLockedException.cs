namespace DocuTrack.Core.Exceptions
{
    public class AccountLockedException : Exception
    {
        public AccountLockedException(): base(
           "The account is temporarily locked due to repeated failed login attempts.")
        {
        }
    }
}
