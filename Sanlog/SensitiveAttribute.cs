using System;

namespace Sanlog
{
    /// <summary>
    /// Specifies that object value represents sensitive data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SensitiveAttribute : Attribute { }
}