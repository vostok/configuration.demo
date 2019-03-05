using System.Collections.Generic;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Primitives;

namespace Vostok.Configuration.Demo
{
    internal class SettingsValidator : ISettingsValidator<ApplicationSettings>
    {
        public IEnumerable<string> Validate(ApplicationSettings settings)
        {
            if (settings.SecuritySettings.AuthenticatorUri.Scheme != "https")
                yield return $"{nameof(settings.SecuritySettings.AuthenticatorUri)}: https scheme required";

            if (settings.StorageSettings.MaxPartSize > 100.Megabytes())
                yield return $"{nameof(settings.StorageSettings.MaxPartSize)}: value is too large, maximum allowed is 100 megabytes";
        }
    }
}