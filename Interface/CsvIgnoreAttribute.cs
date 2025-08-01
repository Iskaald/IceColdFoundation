using System;

namespace IceCold.Interface
{
    /// <summary>
    /// Add this attribute to a field in an IceColdConfig to prevent it from being included in the CSV export.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class CsvIgnoreAttribute : Attribute { }
}