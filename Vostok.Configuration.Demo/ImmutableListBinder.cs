using System;
using System.Collections.Immutable;
using System.Linq;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.Configuration.Demo
{
    internal class ImmutableListBinder<T>: ISettingsBinder<ImmutableList<T>>
    {
        private readonly ISettingsBinder<T> elementBinder;

        public ImmutableListBinder(ISettingsBinder<T> elementBinder)
        {
            this.elementBinder = elementBinder;
        }
        
        public ImmutableList<T> Bind(ISettingsNode rawSettings)
        {
            if (rawSettings == null)
                return ImmutableList<T>.Empty;

            if (!(rawSettings is ArrayNode))
                throw new InvalidOperationException($"A(n) {rawSettings.GetType().Name} cannot be bound to '{typeof(ImmutableList<T>)}'");
            
            return ImmutableList<T>.Empty.AddRange(rawSettings.Children.Select(node => elementBinder.Bind(node)));
        }
    }
}