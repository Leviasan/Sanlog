using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Sanlog.Compliance.Redaction;

namespace Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    public sealed class FormattedLogValuesFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The format string is used to redact sensitive values.
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
        private readonly FormattedLogValuesFormatterOptions _configuration;
        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IRedactorProvider _redactorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified configuration.
        /// </summary>
        /// <param name="redactorProvider">The redactors provider for different data classifications.</param>
        /// <param name="configuration">The configuration of the formatter.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="redactorProvider"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatter(IRedactorProvider redactorProvider, IOptions<FormattedLogValuesFormatterOptions> configuration)
        {
            ArgumentNullException.ThrowIfNull(redactorProvider);
            ArgumentNullException.ThrowIfNull(configuration);
            _redactorProvider = redactorProvider;
            _configuration = configuration.Value;
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
                    if (TryOverrideFormat(arg, formatProvider, _configuration, out var stringValue))
                    {
                        return stringValue;
                    }
                }
                else
                {
                    if (format.Equals(FormatRedacted, StringComparison.Ordinal))
                    {
                        return SensitiveRedactor.RedactedValue;
                    }
                    if (format.Equals(FormatSerialize, StringComparison.Ordinal) && arg is not null)
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
            static bool TryOverrideFormat([NotNullWhen(false)] object? arg, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = arg switch
                {
                    IFormattable formattable => configuration.GetFormat(formattable.GetType()) is string format
                        ? formattable.ToString(format, formatProvider)
                        : null,
                    null => NullValue,
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            static string Serialize(object? obj, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, IRedactorProvider redactorProvider)
            {
                const string EmptyArray = "[]";

                return TryOverrideFormat(obj, formatProvider, configuration, out var stringValue) ? stringValue : obj switch
                {
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => SerializeDictionary(dictionary, formatProvider, configuration, redactorProvider), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => SerializeEnumerable(enumerable, formatProvider, configuration, redactorProvider),
                    _ => SerializeObject(obj, formatProvider, configuration, redactorProvider)
                };

                static string SerializeDictionary(IDictionary dictionary, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    var first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(formatProvider, $"[{Serialize(entry.Key, formatProvider, configuration, redactorProvider)}, {Serialize(entry.Value, formatProvider, configuration, redactorProvider)}]");
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
                static string SerializeEnumerable(IEnumerable enumerable, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    var first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (var value in enumerable)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(Serialize(value, formatProvider, configuration, redactorProvider));
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
                static string SerializeObject(object obj, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, IRedactorProvider redactorProvider)
                {
                    const string EmptyObject = "{}";
                    const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

                    var type = obj.GetType();
                    if (TryGetRedactor(type, redactorProvider, out var redactor))
                        return redactor.Redact(obj, null, formatProvider);

                    StringBuilder? stringBuilder = null;
                    var properties = type.GetProperties(InstancePublic);
                    for (var index = 0; index < properties.Length; ++index)
                    {
                        var property = properties[index];
                        stringBuilder = stringBuilder is null ? new StringBuilder(256).Append('{') : stringBuilder;
                        _ = stringBuilder
                            .Append(' ')
                            .Append(property.Name)
                            .Append(" = ");
                        if (TryGetRedactor(property, redactorProvider, out redactor))
                            _ = stringBuilder.AppendRedacted(redactor, Serialize(property.GetValue(obj), formatProvider, configuration, redactorProvider));
                        _ = stringBuilder.Append(index < properties.Length - 1 ? ',' : ' ');
                    }
                    return stringBuilder?.Append('}').ToString() ?? EmptyObject;
                }
                static bool TryGetRedactor(MemberInfo member, IRedactorProvider redactorProvider, [NotNullWhen(true)] out Redactor? redactor)
                {
                    redactor = null;
                    if (member.IsDefined(typeof(DataClassificationAttribute)))
                    {
                        var attributes = member.GetCustomAttributes<DataClassificationAttribute>();
                        redactor = redactorProvider.GetRedactor(new DataClassificationSet(attributes.Select(x => x.Classification)));
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}