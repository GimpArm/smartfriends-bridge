using Logging.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace SmartFriends.Host
{
    public static class Extensions
    {
        public static ILoggingBuilder AddMemory(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, MemoryLoggerProvider>(x => new MemoryLoggerProvider((category, logLevel) => logLevel >= LogLevel.Information, 100)));
            return builder;
        }
    }
}
