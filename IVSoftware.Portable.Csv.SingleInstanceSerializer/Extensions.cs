using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IVSoftware.Portable.Csv
{
    public static class Extensions
    {
        public static string GetCsvHeader(this Type @type) =>
            string.Join(", ", @type.GetCsvHeaderArray());
        public static string[] GetCsvHeaderArray(this Type @type) =>
            @type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(_ => !Attribute.IsDefined(_, typeof(CsvIgnoreAttribute)))
                .Select(_ => _.Name)
                .ToArray();

        const string IGNORE_ESCAPED_COMMAS_PATTERN = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        public static T FromCsvLine<T>(this Type @type, string csvLine) 
            where T : new()
        {
            if (csvLine == type.GetCsvHeader())
            {
                // Can't make an instance of T from the header row.
                return default;
            }
            else
            {
                var CsvHeaderNames = type.GetCsvHeaderArray();
                var newT = Activator.CreateInstance<T>();
                var values = Regex.Split(csvLine, IGNORE_ESCAPED_COMMAS_PATTERN);
                for (int i = 0; i < CsvHeaderNames.Length; i++)
                {
                    var propertyName = CsvHeaderNames[i];
                    var value = localRemoveOutsideQuotes(values[i]);
                    if (@type.GetProperty(propertyName) is PropertyInfo pi)
                    {
                        if (Attribute.IsDefined(pi, typeof(CsvIgnoreAttribute)))
                        {
                            continue;
                        }
                        else
                        {
                            switch (pi.PropertyType.Name)
                            {
                                case nameof(Int32):
                                    pi.SetValue(newT, Int32.Parse(value));
                                    break;
                                case nameof(String):
                                    pi.SetValue(newT, value);
                                    break;
                                default:
                                    Debug.Assert(false, "An unhandled type has been added to this class.");
                                    break;
                            }
                        }
                    }
                    string localRemoveOutsideQuotes(string s)
                    {
                        if (s.Contains(',') && s.StartsWith("\"") && s.EndsWith("\""))
                        {
                            return s.Substring(1, s.Length - 2);
                        }
                        else return s;
                    }
                }
                return newT;
            }
        }
        [CsvIgnore]
        static string property { get; set; }
        private static Dictionary<Type, KnownTypeInfo> KnownTypeDict { get; } = 
            new Dictionary<Type, KnownTypeInfo>();
        private class KnownTypeInfo
        {
            public string CsvHeader { get; set; }
            public string[] CsvHeaderArray { get; set; }
        }
    }
    public class CsvIgnoreAttribute : Attribute { }
}
