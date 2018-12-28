using System;
using System.IO;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Commons.Testing.Observable;
using Vostok.Configuration.Sources;
using Vostok.Configuration.Sources.File;
using Vostok.Configuration.Sources.Json;

namespace Vostok.Configuration.Demo
{
    [TestFixture]
    internal class Configuration_FunctionalTests
    {
        private const string SettingsJson = "{ 'A': 1, 'B': '2', 'C': {'D': 3} }";
        private static readonly SettingsClass SettingsObject = new SettingsClass
        {
            A = 1,
            B = "2",
            C = new NestedSettingsClass
            {
                D = 3
            }
        };
        
        private ConfigurationProvider provider;
        private JsonStringSource source;

        [SetUp]
        public void SetUp()
        {
            provider = new ConfigurationProvider();
            source = new JsonStringSource(SettingsJson);
        }
        
        [Test]
        public void Get_should_work_correctly_with_preconfigured_source()
        {
            provider.SetupSourceFor<SettingsClass>(source);
            provider.Get<SettingsClass>()
                .Should()
                .BeEquivalentTo(SettingsObject);
        }
        
        [Test]
        public void Get_should_work_correctly_with_custom_source()
        {
            provider.Get<SettingsClass>(source)
                .Should()
                .BeEquivalentTo(SettingsObject);
        }

        [Test]
        public void Observe_should_work_correctly_with_preconfigured_source()
        {
            provider.SetupSourceFor<SettingsClass>(source);
            provider.Observe<SettingsClass>()
                .WaitFirstValue(100.Milliseconds())
                .Should()
                .BeEquivalentTo(SettingsObject);
        }

        [Test]
        public void Observe_should_work_correctly_with_custom_source()
        {
            provider.Observe<SettingsClass>(source)
                .WaitFirstValue(100.Milliseconds())
                .Should()
                .BeEquivalentTo(SettingsObject);
        }
        
        [Test]
        public void ObserveWithErrors_should_work_correctly_with_preconfigured_source()
        {
            provider.SetupSourceFor<SettingsClass>(source);
            provider.ObserveWithErrors<SettingsClass>()
                .WaitFirstValue(100.Milliseconds())
                .Should()
                .BeEquivalentTo((SettingsObject, null as Exception));
        }

        [Test]
        public void ObserveWithErrors_should_work_correctly_with_custom_source()
        {
            provider.ObserveWithErrors<SettingsClass>(source)
                .WaitFirstValue(100.Milliseconds())
                .Should()
                .BeEquivalentTo((SettingsObject, null as Exception));
        }

        [Test]
        public void Get_should_reflect_updates()
        {
            using(var temporaryFile = new TemporaryFile("{ 'a': 1 }"))
            {
                var source = new JsonFileSource(new FileSourceSettings(temporaryFile.FileName){FileWatcherPeriod = 100.Milliseconds()});
                
                provider.Get<SettingsClass>(source)
                    .Should()
                    .BeEquivalentTo(new SettingsClass {A = 1});

                
                File.WriteAllText(temporaryFile.FileName, "{ 'a': 2 }");
                
                Action assertion = () => provider.Get<SettingsClass>(source).Should().BeEquivalentTo(new SettingsClass {A = 2});
                assertion.ShouldPassIn(1.Seconds());
            }
        }

        [Test]
        public void Get_should_throw_errors()
        {
            new Action(() => provider.Get<double>(source))
                .Should()
                .Throw<Exception>();
        }

        [Test]
        public void Combined_and_scoped_sources_should_work_correctly()
        {
            var source1 = new JsonStringSource("{ 'a': [1, 2, 3] }");
            var source2 = new JsonStringSource("{ 'a': [4], 'b': '5' }");

            provider.Get<int[]>(source1.Combine(source2).ScopeTo("a"))
                .Should()
                .BeEquivalentTo(new[]{1, 2, 3, 4});
        }
        
        [Test]
        public void Combined_and_scoped_sources_should_reflect_updates()
        {
            using(var temporaryFile = new TemporaryFile("{ 'a': [1] }"))
            {
                var source1 = new JsonFileSource(new FileSourceSettings(temporaryFile.FileName){FileWatcherPeriod = 100.Milliseconds()});
                var source2 = new JsonStringSource("{ 'a': [3], 'b': '4' }");
                var combinedScopedSource = source1.Combine(source2).ScopeTo("a");
                
                provider.Get<int[]>(combinedScopedSource)
                    .Should()
                    .BeEquivalentTo(new []{1, 3});
                
                File.WriteAllText(temporaryFile.FileName, "{ 'a': [2] }");
                
                Action assertion = () => provider.Get<int[]>(combinedScopedSource)
                    .Should()
                    .BeEquivalentTo(new []{2, 3});
                
                assertion.ShouldPassIn(1.Seconds());
            }
        }
        
        private class SettingsClass
        {
            public int A;
            public string B;
            public NestedSettingsClass C;
        }

        private class NestedSettingsClass
        {
            public int D;
        }
    }
}