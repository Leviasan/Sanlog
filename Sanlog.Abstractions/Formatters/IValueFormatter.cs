using System;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Defines a method that supports custom formatting of the value of an object.
    /// </summary>
    public interface IValueFormatter : ICustomFormatter, IFormatProvider { }
}