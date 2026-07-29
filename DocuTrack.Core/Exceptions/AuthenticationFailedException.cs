using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTrack.Core.Exceptions
{
    public sealed class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException()
        : base("The email or password is incorrect.")
        {
        }
    }
}
