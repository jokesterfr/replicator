using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedAutoPropertyAccessor.Global

#nullable disable
namespace es_replicator.Settings {
    public record EsdbSettings {
        public string ConnectionString { get; init; }
        public string Protocol         { get; init; }
    }

    public record Checkpoint {
        public string Path { get; init; }
    }

    public record SinkSettings : EsdbSettings {
        public int ConcurrencyLimit { get; init; } = 1;
        public int PartitionCount   { get; init; } = 1;
    }

    public record Replicator {
        public EsdbSettings Reader       { get; init; }
        public SinkSettings Sink         { get; init; }
        public bool         Scavenge     { get; init; }
        public Checkpoint   Checkpoint   { get; init; }
        public SinkSettings SinkSettings { get; init; }
    }

    public static class ConfigExtensions {
        public static T GetAs<T>(this IConfiguration configuration) where T : new() {
            T result = new();
            configuration.GetSection(typeof(T).Name).Bind(result);
            return result;
        }

        public static void AddOptions<T>(this IServiceCollection services, IConfiguration configuration)
            where T : class {
            services.Configure<T>(configuration.GetSection(typeof(T).Name));
        }
    }
}
#nullable enable