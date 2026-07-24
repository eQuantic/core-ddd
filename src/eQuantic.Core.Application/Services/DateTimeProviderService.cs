using System.Diagnostics;

namespace eQuantic.Core.Application.Services;

public class DateTimeProviderService : IDateTimeProviderService
{
#if NET8_0
     private readonly TimeProvider _timeProvider;

     public DateTimeProviderService(TimeProvider timeProvider)
     {
          _timeProvider = timeProvider;
     }

     public DateTimeOffset GetLocalNow()
     {
          return _timeProvider.GetLocalNow();
     }

     public DateTimeOffset GetUtcNow()
     {
          return _timeProvider.GetUtcNow();
     }

     public long GetTimestamp()
     {
          return _timeProvider.GetTimestamp();
     }
#else
     public DateTimeOffset GetLocalNow()
     {
          return DateTimeOffset.Now;
     }

     public DateTimeOffset GetUtcNow()
     {
          return DateTimeOffset.UtcNow;
     }

     public long GetTimestamp()
     {
          return Stopwatch.GetTimestamp();
     }
#endif
}