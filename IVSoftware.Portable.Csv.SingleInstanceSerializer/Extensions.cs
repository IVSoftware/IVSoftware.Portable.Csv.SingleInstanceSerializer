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
        /// <summary>
        /// Enumerates the public R/W property names of an instance of @type.
        /// </summary>
        /// <returns>Comma delimited list of names.</returns>
        public static string GetCsvHeader(this Type @type)
        {
            KnownTypeInfo info;
            if (!KnownTypeDict.TryGetValue(@type, out info))
            {
                info = new KnownTypeInfo(@type);
            }
            return info.CsvHeader;
        }
        /// <summary>
        /// Enumerates the public R/W property names of an instance of @type.
        /// </summary>
        /// <returns>String array of names.</returns>
        public static string[] GetCsvHeaderArray(this Type @type)
        {
            KnownTypeInfo info;
            if (!KnownTypeDict.TryGetValue(@type, out info))
            {
                info = new KnownTypeInfo(@type);
            }
            return info.CsvHeaderArray;
        }

        const string IGNORE_ESCAPED_COMMAS_PATTERN = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

        /// <summary>
        /// Deserializes an instance of @type from comma delimited string 
        /// based on names obtained from GetCsvHeader().
        /// </summary>
        /// <returns>Instance of type T</returns>
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

        /// <summary>
        /// Deserializes an instance of @type from comma delimited string 
        /// based on names obtained from GetCsvHeader().
        /// </summary>
        /// <returns>Comma delimited enumuration of public R/W property values.</returns>
        public static string ToCsvLine<T>(this T instance)
        {
            return
                string.Join(
                    ",",
                    instance.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(_=>_.CanRead || _.CanWrite)
                        .Select(_ => localEscape(_.GetValue(instance)?.ToString() ?? string.Empty)));
            string localEscape(string mightHaveCommas)
            {
                if (mightHaveCommas.Contains(","))
                {
                    return $@"""{mightHaveCommas}""";
                }
                else return mightHaveCommas;
            }
        }

        private static Dictionary<Type, KnownTypeInfo> KnownTypeDict { get; } = 
            new Dictionary<Type, KnownTypeInfo>();
        private class KnownTypeInfo
        {
            public KnownTypeInfo(Type type)
            {
                CsvHeaderArray = @type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(_ => !Attribute.IsDefined(_, typeof(CsvIgnoreAttribute)))
                    .Where(_ => _.CanRead || _.CanWrite)
                    .Select(_ => _.Name)
                    .ToArray();

                CsvHeader = string.Join(", ", CsvHeaderArray);
            }
            public string CsvHeader { get; }
            public string[] CsvHeaderArray { get;  }
        }
    }
    public class CsvIgnoreAttribute : Attribute { }
}
