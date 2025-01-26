using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the <see cref="FormattedLogValuesFormatter"/>.
    /// </summary>
    public sealed class FormattedLogValuesFormatterOptions
    {
        /// <summary>
        /// The <see cref="DateTime"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _dateTimeFormat;
        /// <summary>
        /// The <see cref="DateTimeOffset"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _dateTimeOffsetFormat;
        /// <summary>
        /// The <see cref="Enum"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _enumFormat;
        /// <summary>
        /// The <see cref="float"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _singleFormat;
        /// <summary>
        /// The <see cref="double"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _doubleFormat;
        /// <summary>
        /// The sensitive properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, HashSet<string>> _dictionary = [];

        /// <summary>
        /// Gets a value indicating whether the configuration is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }
        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> format string.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? DateTimeFormat
        {
            get => _dateTimeFormat;
            set
            {
                CheckReadOnly();
                _dateTimeFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="DateTimeOffset"/> format string.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? DateTimeOffsetFormat
        {
            get => _dateTimeOffsetFormat;
            set
            {
                CheckReadOnly();
                _dateTimeOffsetFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="Enum"/> format string.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        [StringSyntax(StringSyntaxAttribute.EnumFormat)]
        public string? EnumFormat
        {
            get => _enumFormat;
            set
            {
                CheckReadOnly();
                _enumFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="float"/> format string.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        [StringSyntax(StringSyntaxAttribute.NumericFormat)]
        public string? SingleFormat
        {
            get => _singleFormat;
            set
            {
                CheckReadOnly();
                _singleFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="double"/> format string.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        [StringSyntax(StringSyntaxAttribute.NumericFormat)]
        public string? DoubleFormat
        {
            get => _doubleFormat;
            set
            {
                CheckReadOnly();
                _doubleFormat = value;
            }
        }

        /// <summary>
        /// Registers a property whose value belongs to sensitive data.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool AddSensitive(Type type, string property)
        {
            var d = Mapper<FormatException>.GetProperty(x => x.StackTrace);
            var e = AddSensitive<FormatException, string>(x => x.HResult);



            CheckReadOnly(); // InvalidOperationException
            ArgumentNullException.ThrowIfNull(property);
            return _dictionary.TryGetValue(type, out var hashset) ? hashset.Add(property) : _dictionary.TryAdd(type, [property]);
        }
        /// <summary>
        /// Makes the configuration read-only.
        /// </summary>
        public void MakeReadOnly() => IsReadOnly = true;
        /// <summary>
        /// Throws an exception if the configuration is read-only.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("The configuration is read-only.");
        }

        internal bool AddSensitive<T, P>(Expression<Func<T, P>> expression)
        {
            var ex = new FormatException();
            var c = InstanceMapper.GetProperty(ex, x => x.HResult);

            var e = ex.GetProperty(x => x.StackTrace);

            var p = Mapper<T>.GetProperty(expression);
            return AddSensitive(typeof(T), p.Name);
        }
    }

    internal static class Mapper<T>
    {
        public static PropertyInfo GetProperty<P>(Expression<Func<T, P>> expression)
        {
            return expression.Body is MemberExpression member && member.Member is PropertyInfo property
                ? property
                : throw new ArgumentException("Expression is not a Property", nameof(expression));
        }
    }
    internal static class InstanceMapper
    {
        public static PropertyInfo GetProperty<T, P>(this T _, Expression<Func<T, P>> expression)
        {
            return expression.Body is MemberExpression member && member.Member is PropertyInfo property
                ? property
                : throw new ArgumentException("Expression is not a Property", nameof(expression));
        }
    }
}