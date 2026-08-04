using DocuTrack.Core.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTrack.Api.IntegrationTests.Authentication
{
    public sealed class TestIdentityTransaction: IIdentityTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
        
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
