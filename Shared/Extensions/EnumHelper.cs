using System;
using System.Reflection;

namespace starterapi.Helpers
{
    public static class EnumHelper
    {
        public static string GetName(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attribute = fieldInfo.GetCustomAttribute<MetadataAttribute>();
            return attribute?.Name ?? value.ToString();
        }

        public static string GetValue(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attribute = fieldInfo.GetCustomAttribute<MetadataAttribute>();
            return attribute?.Value ?? value.ToString();
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class MetadataAttribute : Attribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public MetadataAttribute(string name = null, string value = null)
        {
            Name = name;
            Value = value;
        }
    }
}