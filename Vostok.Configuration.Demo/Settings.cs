using System;
using System.Collections.Immutable;
using Vostok.Configuration.Abstractions.Attributes;
using Vostok.Configuration.Primitives;

namespace Vostok.Configuration.Demo
{
    [ValidateBy(typeof(SettingsValidator))]
    internal class ApplicationSettings
    {
        [Required]
        public StorageSettings StorageSettings { get; }
            
        [Required]
        public SecuritySettings<string> SecuritySettings { get; }
    }
    
    [RequiredByDefault]
    internal class StorageSettings
    {
        public string[] ClusterNames { get; }
        public TimeSpan GrayPeriod { get; }
        
        [Optional]
        public bool EnableNewCluster { get; }
        [Optional]
        public int RetryAttempts { get; } = 5;
        [Optional]
        public DataSize MaxPartSize { get; } = 10.Megabytes();
    }

    internal class SecuritySettings<T>
    {
        [Required]
        public Uri AuthenticatorUri;
        public ImmutableList<T> Extensions;
    }
}