using DocuTrack.Application.Abstractions.Time;

namespace DocuTrack.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow =>
            DateTimeOffset.UtcNow;
    }
}
