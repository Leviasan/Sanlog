using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    internal sealed class FormattedLogValuesFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The format string is used to redact sensitive data.
        /// </summary>
        public const string FormatRedacted = "R";
        /// <summary>
        /// The format string is used to serialize public properties to a string representation.
        /// </summary>
        public const string FormatSerialize = "S";
        /// <summary>
        /// The message format that represents a null value.
        /// </summary>
        public const string NullValue = "(null)";

        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LoggerFormatterOptions _configuration;
        /// <summary>
        /// The redactors provider for different data classifications.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IRedactorProvider _redactorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified redactor provider and the configuration of the formatter.
        /// </summary>
        /// <param name="redactorProvider">The redactors provider for different data classifications.</param>
        /// <param name="options">The configuration of the formatter.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="redactorProvider"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatter(IRedactorProvider redactorProvider, LoggerFormatterOptions options)
        {
            ArgumentNullException.ThrowIfNull(redactorProvider);
            ArgumentNullException.ThrowIfNull(options);
            _redactorProvider = redactorProvider;
            _configuration = options;
        }

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : _configuration.CultureInfo?.GetFormat(formatType);
        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (Equals(formatProvider))
            {
                if (string.IsNullOrEmpty(format))
                {
                    if (TryOverrideFormat(arg, formatProvider, _configuration, out string? stringValue))
                    {
                        return stringValue;
                    }
                }
                else if (arg is not null)
                {
                    if (format.Equals(FormatRedacted, StringComparison.Ordinal) && TryGetRedactor(arg.GetType(), _redactorProvider, out Redactor? redactor))
                    {
                        return redactor.Redact(arg, null, formatProvider);
                    }
                    if (format.Equals(FormatSerialize, StringComparison.Ordinal))
                    {
                        return Serialize(arg, formatProvider, _configuration, _redactorProvider);
                    }
                }
            }
            return DefaultFallback(format, arg, Equals(formatProvider) ? _configuration.CultureInfo : formatProvider);

            static string DefaultFallback(string? format, object? arg, IFormatProvider? formatProvider)
            {
                return arg switch
                {
                    IFormattable formattable => formattable.ToString(format, formatProvider),
                    _ => Convert.ToString(arg, formatProvider) ?? string.Empty
                };
            };
            static bool TryOverrideFormat([NotNullWhen(false)] object? arg, IFormatProvider? formatProvider, LoggerFormatterOptions configuration, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = arg switch
                {
                    // supports LoggerFormatterOptions.OverrideFormat + LoggerFormatterOptions.RegisterFormatter
                    object obj => configuration.GetFormatter(obj.GetType()) is Func<object?, string?> callback ? callback.Invoke(obj) : null,
                    // format of the null value
                    null => NullValue
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            static bool TryGetRedactor(MemberInfo member, IRedactorProvider redactorProvider, [NotNullWhen(true)] out Redactor? redactor)
            {
                redactor = null;
                if (member.IsDefined(typeof(DataClassificationAttribute)))
                {
                    IEnumerable<DataClassificationAttribute> attributes = member.GetCustomAttributes<DataClassificationAttribute>();
                    redactor = redactorProvider.GetRedactor(new DataClassificationSet(attributes.Select(x => x.Classification)));
                    return true;
                }
                return false;
            }
            static string Serialize(object? obj, IFormatProvider? formatProvider, LoggerFormatterOptions configuration, IRedactorProvider redactorProvider)
            {
                const string EmptyArray = "[]";

                return TryOverrideFormat(obj, formatProvider, configuration, out string? stringValue) ? stringValue : obj switch
                {
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => SerializeDictionary(dictionary, formatProvider, configuration, redactorProvider), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => SerializeEnumerable(enumerable, formatProvider, configuration, redactorProvider),
                    _ => SerializeObject(obj, formatProvider, configuration, redactorProvider)
                };

                static string SerializeDictionary(IDictionary dictionary, IFormatProvider? formatProvider, LoggerFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    bool first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(formatProvider, $"[{Serialize(entry.Key, formatProvider, configuration, redactorProvider)}," +
                            $" {Serialize(entry.Value, formatProvider, configuration, redactorProvider)}]");
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
                static string SerializeEnumerable(IEnumerable enumerable, IFormatProvider? formatProvider, LoggerFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    bool first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (object? value in enumerable)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(Serialize(value, formatProvider, configuration, redactorProvider));
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
                static string SerializeObject(object obj, IFormatProvider? formatProvider, LoggerFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    const string EmptyObject = "{}";
                    const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

                    Type type = obj.GetType();
                    if (TryGetRedactor(type, redactorProvider, out Redactor? redactor))
                        return redactor.Redact(obj, null, formatProvider);

                    StringBuilder? stringBuilder = null;
                    PropertyInfo[] properties = type.GetProperties(InstancePublic);
                    for (int index = 0; index < properties.Length; ++index)
                    {
                        PropertyInfo property = properties[index];
                        stringBuilder = stringBuilder is null ? new StringBuilder(256).Append('{') : stringBuilder;
                        _ = stringBuilder
                            .Append(' ')
                            .Append(property.Name)
                            .Append(" = ");
                        _ = TryGetRedactor(property, redactorProvider, out redactor)
                            ? stringBuilder.AppendRedacted(redactor, Serialize(property.GetValue(obj), formatProvider, configuration, redactorProvider))
                            : stringBuilder.Append(Serialize(property.GetValue(obj), formatProvider, configuration, redactorProvider));
                        _ = stringBuilder.Append(index < properties.Length - 1 ? ',' : ' ');
                    }
                    return stringBuilder?.Append('}').ToString() ?? (type.IsPrimitive && obj.ToString() is string primitive ? primitive : EmptyObject);
                }
            }
        }
    }
}