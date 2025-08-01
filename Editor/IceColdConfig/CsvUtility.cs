using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace IceCold.Interface
{
    public static class CsvUtility
    {
        // Regex to parse a CSV row, handles quoted fields with commas.
        private static readonly Regex CsvRowRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        /// <summary>
        /// Parses a CSV string into a list of dictionaries, where each dictionary represents a row.
        /// </summary>
        public static List<Dictionary<string, string>> ParseCsv(string csv)
        {
            var result = new List<Dictionary<string, string>>();
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                Debug.LogWarning("[CsvUtility] CSV has no data rows to parse.");
                return result;
            }

            var headers = CsvRowRegex.Split(lines[0]).Select(h => h.Trim().Trim('"')).ToArray();

            for (var i = 1; i < lines.Length; i++)
            {
                var values = CsvRowRegex.Split(lines[i]);
                var row = new Dictionary<string, string>();
                for (var j = 0; j < headers.Length && j < values.Length; j++)
                {
                    // Clean up value: remove surrounding quotes and un-escape double-quotes
                    var value = values[j].Trim().Trim('"');
                    value = value.Replace("\"\"", "\"");
                    row[headers[j]] = value;
                }
                result.Add(row);
            }
            return result;
        }

        /// <summary>
        /// Populates the fields of an object from the first data row of a CSV string.
        /// </summary>
        public static void FillObjectFromCsv<T>(T obj, string csv) where T : class
        {
            var data = ParseCsv(csv);
            if (data.Count == 0) return;

            var firstRow = data[0];
            var fields = GetSerializableFields(obj.GetType());

            foreach (var field in fields)
            {
                if (!firstRow.TryGetValue(field.Name, out var value)) continue;

                try
                {
                    // handles enums, bools, numbers etc.
                    var converter = TypeDescriptor.GetConverter(field.FieldType);
                    var convertedValue = converter.ConvertFromInvariantString(value);
                    field.SetValue(obj, convertedValue);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CsvUtility] Could not convert value '{value}' for field '{field.Name}' to type '{field.FieldType.Name}'. Error: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Converts an object's serializable fields into a CSV string.
        /// </summary>
        public static string ToCsv<T>(T obj) where T : class
        {
            var fields = GetSerializableFields(obj.GetType());
            var sb = new StringBuilder();

            // Headers
            var fieldInfos = fields as FieldInfo[] ?? fields.ToArray();
            sb.AppendLine(string.Join(",", fieldInfos.Select(f => f.Name)));

            // Values
            var values = fieldInfos.Select(f =>
            {
                var value = f.GetValue(obj);
                return EscapeCsvValue(value?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));

            return sb.ToString();
        }

        /// <summary>
        /// Gets all fields that should be serialized: public fields and private fields with [SerializeField],
        /// excluding those with [CsvIgnore].
        /// </summary>
        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                                && field.GetCustomAttribute<CsvIgnoreAttribute>() == null);
        }

        /// <summary>
        /// Escapes a string value for safe inclusion in a CSV file.
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            // If the value contains a comma, a quote, or a newline, wrap it in double quotes.
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                // Also, double up any existing double quotes.
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}