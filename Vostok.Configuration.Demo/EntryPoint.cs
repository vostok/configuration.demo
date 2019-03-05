using System;
using System.Collections.Generic;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.Extensions.Observable;
using Vostok.Configuration.Binders;
using Vostok.Configuration.Logging;
using Vostok.Configuration.Sources;
using Vostok.Configuration.Sources.CommandLine;
using Vostok.Configuration.Sources.Environment;
using Vostok.Configuration.Sources.Json;
using Vostok.Logging.Console;

namespace Vostok.Configuration.Demo
{
    class EntryPoint
    {
        private static ConfigurationProvider provider;

        static void Main(string[] args)
        {
            SetupConfigurationProvider();

            var configSource = new JsonFileSource("config.json");
            
            var source = configSource
                .CombineWith(new EnvironmentVariablesSource())
                .CombineWith(new CommandLineSource(args));
            provider.SetupSourceFor<ApplicationSettings>(source);
            
            SetupApplication();

            PrintOptions(configSource);

            Console.ReadKey();
        }
        
        private static void SetupConfigurationProvider()
        {
            var log = new ConsoleLog();
            
            var providerSettings = new ConfigurationProviderSettings
                {
                    Binder = new DefaultSettingsBinder().WithCustomBinder(typeof(ImmutableListBinder<>), _ => true)
                }
                .WithErrorLogging(log)
                .WithSettingsLogging(log);
            provider = new ConfigurationProvider(providerSettings);
        }
        
        private static void SetupApplication()
        {
            var settings = provider.Get<ApplicationSettings>();
            var application = new Application(settings);
            provider.Observe<ApplicationSettings>().Subscribe(application.UpdateSettings);
        }

        private static void PrintOptions(IConfigurationSource configSource)
        {
            var optionsSource = configSource.ScopeTo("options", "application");
            var options = provider.Get<Dictionary<string, bool>>(optionsSource);
            
            Console.WriteLine("Options:");
            Console.WriteLine(ConfigurationPrinter.Print(options));
        }
    }
}