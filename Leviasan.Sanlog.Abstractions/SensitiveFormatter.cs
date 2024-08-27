using System;
using System.Collections;
using System.Collections.Generic;

namespace Leviasan.Sanlog
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        public const string RedactedValue = "[Redacted]";

        private readonly IReadOnlyList<KeyValuePair<string, object?>> _dictionary;
        private readonly SensitiveConfiguration _configuration;

        protected SensitiveFormatter(IReadOnlyList<KeyValuePair<string, object?>> dictionary, SensitiveConfiguration configuration)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IFormatProvider? FormatProvider { get; set; }

        public abstract string Format(string? format, object? arg, IFormatProvider? formatProvider);
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : FormatProvider?.GetFormat(formatType);
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var key = _dictionary[index].Key;
            var value = ProcessSensitiveObject(key, _dictionary[index].Value, redacted);
            return KeyValuePair.Create(key, value);

            object? ProcessSensitiveObject(string key, object? value, bool redacted)
            {
                return redacted && _configuration.Contains(typeof(object), key) ? RedactedValue : SensitiveObject(value, redacted);

                object? SensitiveObject(object? value, bool redacted)
                {
                    return value switch
                    {
                        string stringValue => stringValue, // string implements IEnumerable so must be process before
                        IDictionary dictionary => SensitiveDictionary(dictionary, redacted), // IDictionary implements IEnumerable so must be process before
                        IEnumerable enumerable => SensitiveEnumerable(enumerable, redacted),
                        _ => value
                    };
                    IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted)
                    {
                        var newdict = new Dictionary<object, object?>(dictionary.Count);
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            var key = Format(null, entry.Key, this);
                            var newvalue = redacted && _configuration.Contains(typeof(DictionaryEntry), key) ? RedactedValue : entry.Value;
                            newdict.Add(entry.Key, newvalue);
                        }
                        return newdict;
                    }
                    IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted)
                    {
                        var newlist = new List<object?>();
                        foreach (var entry in enumerable)
                            newlist.Add(SensitiveObject(entry, redacted));
                        return newlist;
                    }
                }
            }
        }
        public KeyValuePair<string, object?> GetObject(string name, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(name);
            for (var index = 0; index < _dictionary.Count; ++index)
                if (_dictionary[index].Key.Equals(name, StringComparison.Ordinal)) return GetObject(index, redacted);
            throw new KeyNotFoundException();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}