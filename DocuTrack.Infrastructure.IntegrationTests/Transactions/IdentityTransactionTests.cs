using DocuTrack.Infrastructure.Identity;
using DocuTrack.Infrastructure.IntegrationTests.Collections;
using DocuTrack.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.IntegrationTests.Transactions
{
    [Collection(DatabaseCollection.Name)]
    public sealed class IdentityTransactionTests
    : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;

        public IdentityTransactionTests(
            DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            await _fixture.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task RollbackAsync_UserInsertedInsideTransaction_IsNotPersisted()
        {
            Guid userId = Guid.NewGuid();

            await using (DocuTrackDbContext context =
                         _fixture.CreateDbContext())
            {
                EfIdentityTransactionFactory factory =
                    new(context);

                await using var transaction =
                    await factory.BeginAsync();

                ApplicationUser user = new()
                {
                    Id = userId,
                    UserName = "rollback@doctrack.com",
                    NormalizedUserName =
                        "ROLLBACK@DOCTRACK.COM",
                    Email = "rollback@doctrack.com",
                    NormalizedEmail =
                        "ROLLBACK@DOCTRACK.COM",
                    FullName = "Rollback User",
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                context.Users.Add(user);

                await context.SaveChangesAsync();

                await transaction.RollbackAsync();
            }

            await using DocuTrackDbContext verificationContext =
                _fixture.CreateDbContext();

            bool exists =
                await verificationContext.Users
                    .AnyAsync(user => user.Id == userId);

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task CommitAsync_UserInsertedInsideTransaction_IsPersisted()
        {
            Guid userId = Guid.NewGuid();

            await using (DocuTrackDbContext context =
                         _fixture.CreateDbContext())
            {
                EfIdentityTransactionFactory factory =
                    new(context);

                await using var transaction =
                    await factory.BeginAsync();

                ApplicationUser user1 = new()
                {
                    Id = userId,
                    UserName = "commit@doctrack.com",
                    NormalizedUserName =
                        "COMMIT@DOCTRACK.COM",
                    Email = "commit@doctrack.com",
                    NormalizedEmail =
                        "COMMIT@DOCTRACK.COM",
                    FullName = "Commit User",
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                context.Users.Add(user1);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }

            await using DocuTrackDbContext verificationContext =
                _fixture.CreateDbContext();

            ApplicationUser? user =
                await verificationContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.Id == userId);

            user.Should().NotBeNull();
            user!.Email.Should().Be("commit@doctrack.com");
        }
    }
}
