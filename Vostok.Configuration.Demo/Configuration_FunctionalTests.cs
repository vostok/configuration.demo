using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Commons.Testing.Observable;
using Vostok.Configuration.Abstractions.Merging;
using Vostok.Configuration.Sources;
using Vostok.Configuration.Sources.Constant;
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
        public void Get_should_reflect_updates()
        {
            using(var temporaryFile = new TemporaryFile("{ 'a': 1 }"))
            {
                var source = CreateJsonFileSource(temporaryFile.FileName);
                
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

            provider.Get<int[]>(source1.CombineWith(source2, new SettingsMergeOptions{ArrayMergeStyle = ArrayMergeStyle.Concat}).ScopeTo("a"))
                .Should()
                .BeEquivalentTo(new[]{1, 2, 3, 4});
        }
        
        [Test]
        public void Combined_and_scoped_sources_should_reflect_updates()
        {
            using(var temporaryFile = new TemporaryFile("{ 'a': [1] }"))
            {
                var source1 = CreateJsonFileSource(temporaryFile.FileName);
                var source2 = new JsonStringSource("{ 'a': [3], 'b': '4' }");
                var combinedScopedSource = source1.CombineWith(source2, new SettingsMergeOptions{ArrayMergeStyle = ArrayMergeStyle.Concat}).ScopeTo("a");
                
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

        [Test]
        public void Combine_and_scope_with_failed_source_should_fail()
        {
            var source1 = new JsonStringSource("{ 'a': [1] }");
            var source2 = new LazyConstantSource(() => throw new FormatException());

            var combinedScopedSource = source1.CombineWith(source2).ScopeTo("a");

            new Action(() => provider.Get<int[]>(combinedScopedSource)).Should().Throw<FormatException>();
        }

        [Test]
        public void Observe_should_reflect_updates_correctly()
        {
            var observer = new TestObserver<Dictionary<string, int>>();
            var expectedSettings = new List<Dictionary<string, int>>();
            using(var temporaryFile = new TemporaryFile())
            {
                var source = CreateJsonFileSource(temporaryFile.FileName);

                using (provider.Observe<Dictionary<string, int>>(source).Subscribe(observer))
                {
                    Action assertion = () => observer.Values.Should().BeEquivalentTo(expectedSettings, options => options.WithStrictOrdering());
                    
                    expectedSettings.Add(new Dictionary<string, int>());
                    assertion.ShouldPassIn(500.Milliseconds());
                    
                    File.WriteAllText(temporaryFile.FileName, "{ 'a': 1 }");
                    expectedSettings.Add(new Dictionary<string, int> {["a"] = 1});
                    assertion.ShouldPassIn(500.Milliseconds());
                    
                    File.WriteAllText(temporaryFile.FileName, "error");
                    assertion.ShouldNotFailIn(500.Milliseconds());
                    
                    File.WriteAllText(temporaryFile.FileName, "{ 'a': 2 }");
                    expectedSettings.Add(new Dictionary<string, int> {["a"] = 2});
                    assertion.ShouldPassIn(500.Milliseconds());
                }
            }
        }

        [Test]
        public void Get_should_work_correctly_with_fresh_custom_sources()
        {
            const int sourcesCount = 10000;
            
            using(var temporaryFile = new TemporaryFile("{ 'a': 1 }"))
            {
                for (var i = 0; i < sourcesCount; i++)
                    provider.Get<Dictionary<string, int>>(CreateJsonFileSource(temporaryFile.FileName))["a"].Should().Be(1);

                File.WriteAllText(temporaryFile.FileName, "{ 'a': 2 }");
                Thread.Sleep(500.Milliseconds());
                
                for (var i = 0; i < sourcesCount; i++)
                    provider.Get<Dictionary<string, int>>(CreateJsonFileSource(temporaryFile.FileName))["a"].Should().Be(2);
            }
        }

        [Test]
        public void Observe_should_work_correctly_with_fresh_custom_sources()
        {
            const int sourcesCount = 10000;
            
            var observer = new TestObserver<int>();
            var subscriptions = new List<IDisposable>();
            var expectedSettings = new List<int>();

            try
            {
                using(var temporaryFile = new TemporaryFile("{ 'a': 1 }"))
                {
                    Action assertion = () => observer.Values.Should().Equal(expectedSettings);
                
                    for (var i = 0; i < sourcesCount; i++)
                        subscriptions.Add(provider.Observe<int>(CreateJsonFileSource(temporaryFile.FileName).ScopeTo("a")).Subscribe(observer));
                    expectedSettings.AddRange(Enumerable.Range(0, sourcesCount).Select(_ => 1));
                
                    assertion.ShouldPassIn(500.Milliseconds());
                    
                    File.WriteAllText(temporaryFile.FileName, "error");
                    assertion.ShouldNotFailIn(500.Milliseconds());

                    File.WriteAllText(temporaryFile.FileName, "{ 'a': 2 }");
                    expectedSettings.AddRange(Enumerable.Range(0, sourcesCount).Select(_ => 2));
                    assertion.ShouldPassIn(500.Milliseconds());
                }
            }
            finally
            {
                subscriptions.ForEach(disposable => disposable.Dispose());
            }
        }

        [Test]
        public void Combine_and_scope_should_work_correctly_when_same_source()
        {
            var source = new JsonStringSource("{ 'a': { 'c': [1, 2, 3] }, 'b': { 'c': [4, 5] } }");

            provider.Get<int[]>(source.ScopeTo("a").CombineWith(source.ScopeTo("b"), new SettingsMergeOptions{ArrayMergeStyle = ArrayMergeStyle.Concat}).ScopeTo("c"))
                .Should()
                .BeEquivalentTo(new[]{1, 2, 3, 4, 5});
        }
        
        private JsonFileSource CreateJsonFileSource(string fileName)
        {
            return new JsonFileSource(new FileSourceSettings(fileName){FileWatcherPeriod = 100.Milliseconds()});
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