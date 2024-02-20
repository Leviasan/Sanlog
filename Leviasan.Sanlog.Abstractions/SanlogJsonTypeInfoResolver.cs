using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Defines the reflection-based JSON contract resolver with overridden handling of exception properties.
    /// </summary>
    internal sealed class SanlogJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        /// <summary>
        /// The collection of the ignored exception properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly KeyValuePair<Type, string>[] IgnoredExceptionProperties = [
            KeyValuePair.Create(typeof(string), nameof(Exception.Message)),
            KeyValuePair.Create(typeof(int), nameof(Exception.HResult)),
            KeyValuePair.Create(typeof(string), nameof(Exception.StackTrace)),
            KeyValuePair.Create(typeof(string), nameof(Exception.Source)),
            KeyValuePair.Create(typeof(string), nameof(Exception.HelpLink)),
            KeyValuePair.Create(typeof(IDictionary), nameof(Exception.Data)),
            KeyValuePair.Create(typeof(MethodBase), nameof(Exception.TargetSite)),
            KeyValuePair.Create(typeof(Exception), nameof(Exception.InnerException))];

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogJsonTypeInfoResolver"/> class.
        /// </summary>
        public SanlogJsonTypeInfoResolver() => Modifiers.Add(static (typeInfo) =>
        {
            if (!typeof(Exception).IsAssignableFrom(typeInfo.Type)) return;
            foreach (var propertyInfo in typeInfo.Properties)
            {
                if (IgnoredExceptionProperties.Contains(KeyValuePair.Create(propertyInfo.PropertyType, propertyInfo.Name)))
                    propertyInfo.ShouldSerialize = static (obj, value) => false;
            }
        });
    }
}