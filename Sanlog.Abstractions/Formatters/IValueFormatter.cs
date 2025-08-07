using System;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Defines methods that supports custom formatting of the object.
    /// </summary>
    public interface IValueFormatter : ICustomFormatter, IFormatProvider { }
}