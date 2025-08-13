using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace IceCold.Interface
{
    /// <summary>
    /// A utility class for parsing from and serializing to Comma-Separated Values (CSV) format.
    /// Designed to work with data structured like a spreadsheet, with a header row defining field names.
    /// </summary>
    public static class CsvUtility
    {
        #region --- Deserialization / Parsing ---

        /// <summary>
        /// Parses a CSV string into a list of dictionaries. Each dictionary represents a row,
        /// with keys corresponding to the header columns. This is the primary parsing method.
        /// </summary>
        /// <param name="csvText">The string containing the CSV data.</param>
        /// <returns>A List of Dictionaries, where each dictionary is a row.</returns>
        public static List<Dictionary<string, string>> Parse(string csvText)
        {
            var result = new List<Dictionary<string, string>>();

            if (string.IsNullOrEmpty(csvText))
            {
                Debug.LogWarning("[CsvUtility] CSV text is null or empty. Cannot parse.");
                return result;
            }

            // Standardize line endings to \n for easier splitting
            var lines = csvText.Replace("\r\n", "\n").Split('\n');

            // Filter out empty lines that might result from trailing newlines
            var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

            if (nonEmptyLines.Count < 2)
            {
                Debug.LogWarning("[CsvUtility] CSV must have at least a header row and one data row.");
                return result;
            }

            var headers = ParseCsvLine(nonEmptyLines[0]).Select(h => h.Trim()).ToArray();

            for (var i = 1; i < nonEmptyLines.Count; i++)
            {
                var values = ParseCsvLine(nonEmptyLines[i]);
                var row = new Dictionary<string, string>();

                for (var j = 0; j < headers.Length; j++)
                {
                    if (j < values.Count)
                    {
                        row[headers[j]] = values[j];
                    }
                    else
                    {
                        // Handle cases where a row has fewer columns than the header
                        row[headers[j]] = string.Empty;
                        Debug.LogWarning($"[CsvUtility] Row {i + 1} is missing data for header '{headers[j]}'.");
                    }
                }
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Deserializes a list of pre-parsed dictionaries into a list of strongly-typed objects.
        /// It uses reflection to map dictionary keys to the serializable fields of the target type T.
        /// </summary>
        public static List<T> Deserialize<T>(List<Dictionary<string, string>> preParsedData) where T : new()
        {
            var objectList = new List<T>();
            var fields = GetSerializableFields(typeof(T)).ToArray();

            if (!fields.Any())
            {
                Debug.LogWarning($"[CsvUtility] Type '{typeof(T).Name}' has no serializable fields to deserialize.");
                return objectList;
            }

            foreach (var rowData in preParsedData)
            {
                var instance = new T();
                foreach (var field in fields)
                {
                    if (rowData.TryGetValue(field.Name, out var stringValue))
                    {
                        try
                        {
                            var convertedValue = ConvertValue(stringValue, field.FieldType);
                            if (convertedValue != null)
                            {
                                field.SetValue(instance, convertedValue);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[CsvUtility] Could not set value for field '{field.Name}'. Value: '{stringValue}'. Error: {e.Message}");
                        }
                    }
                }
                objectList.Add(instance);
            }
            return objectList;
        }
        
        /// <summary>
        /// Deserializes a CSV string into a list of strongly-typed objects.
        /// This is a convenience method that combines Parse and Deserialize.
        /// </summary>
        public static List<T> Deserialize<T>(string csvText) where T : new()
        {
            var parsedData = Parse(csvText);
            return Deserialize<T>(parsedData);
        }

        #endregion

        #region --- Serialization / Writing ---

        /// <summary>
        /// Serializes a collection of objects into a CSV string.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the collection.</typeparam>
        /// <param name="objects">The collection of objects to serialize.</param>
        /// <returns>A CSV formatted string.</returns>
        public static string Serialize<T>(IEnumerable<T> objects)
        {
            var objectList = objects.ToList();
            if (!objectList.Any())
            {
                return "";
            }

            var sb = new StringBuilder();
            var fields = GetSerializableFields(typeof(T)).ToArray();

            // Header Row
            var headers = fields.Select(f => EscapeCsvValue(f.Name));
            sb.AppendLine(string.Join(",", headers));

            // Data Rows
            foreach (var obj in objectList)
            {
                var values = fields.Select(f =>
                {
                    var value = f.GetValue(obj);
                    string stringValue;
                    if (value is IList list)
                    {
                        var listItems = list.Cast<object>().Select(item => item?.ToString() ?? "");
                        stringValue = string.Join(";", listItems);
                    }
                    else
                    {
                        stringValue = value?.ToString() ?? "";
                    }
                    return EscapeCsvValue(stringValue);
                });
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// A robust parser for a single line of a CSV file. Handles quoted fields containing commas and escaped quotes.
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Check if this is an escaped quote ("") or the end of the quoted field
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentField.Append('"'); // It's an escaped quote, so add one quote to the field
                            i++; // Skip the next quote
                        }
                        else
                        {
                            inQuotes = false; // It's the end of the quoted field
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '"' && currentField.Length == 0)
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }
            fields.Add(currentField.ToString());

            // Trim quotes from unquoted fields
            // Example: "value" should be value. But value" should remain value"
            return fields.Select(f => f.Trim().TrimStart('"').TrimEnd('"')).ToList();
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
        /// Wraps the value in quotes if it contains a comma, quote, or newline, and escapes internal quotes.
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // If the value contains a comma, a quote, or a newline, it needs to be quoted.
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                // Double up any existing double quotes within the value.
                var escapedValue = value.Replace("\"", "\"\"");
                return $"\"{escapedValue}\"";
            }
            return value;
        }
        
        /// <summary>
        /// Robustly tries to parse a string into a float, handling both '.' and ',' as potential decimal separators.
        /// This method is culture-agnostic and safe for parsing data from files like CSVs.
        /// </summary>
        /// <param name="stringValue">The string to parse.</param>
        /// <param name="value">The output float if parsing is successful.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        public static bool TryGetFloat(string stringValue, out float value)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                value = 0f;
                return false;
            }
            
            const NumberStyles style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;
            
            if (float.TryParse(stringValue, style, culture, out value))
            {
                return true;
            }
            
            if (stringValue.Contains(","))
            {
                var sanitizedValue = stringValue.Replace(',', '.');
                if (float.TryParse(sanitizedValue, style, culture, out value))
                {
                    return true;
                }
            }

            value = 0f;
            Debug.LogWarning($"[CsvUtility] Failed to parse '{stringValue}' as a float. Using default value {value}.");
            return false;
        }
        
        /// <summary>
        /// Robustly tries to parse a string into a double, handling both '.' and ',' as decimal separators.
        /// This method is culture-agnostic, making it safe for parsing data from files like CSVs.
        /// </summary>
        /// <param name="stringValue">The string to parse.</param>
        /// <param name="value">The output double if parsing is successful.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        public static bool TryGetDouble(string stringValue, out double value)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                value = 0f;
                return false;
            }
            
            const NumberStyles style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;
            
            if (double.TryParse(stringValue, style, culture, out value))
            {
                return true;
            }
            
            if (stringValue.Contains(","))
            {
                var sanitizedValue = stringValue.Replace(',', '.');
                if (double.TryParse(sanitizedValue, style, culture, out value))
                {
                    return true;
                }
            }

            value = 0f;
            Debug.LogWarning($"[CsvUtility] Failed to parse '{stringValue}' as a double. Using default value {value}.");
            return false;
        }

        /// <summary>
        /// Safely parses a separated string into a List of a specified type.
        /// Skips any elements that cannot be parsed and logs a warning.
        /// </summary>
        /// <typeparam name="T">The target type to convert each element to.</typeparam>
        /// <param name="stringValue">The string containing the separated values (e.g., "1;2;abc;3").</param>
        /// <param name="separator">The separator character(s).</param>
        /// <returns>A List of successfully parsed values.</returns>
        public static List<T> GetList<T>(string stringValue, string separator)
        {
            var result = new List<T>();
            
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return result;
            }
            
            var elements = stringValue.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var element in elements)
            {
                try
                {
                    var trimmedElement = element.Trim();
            
                    var convertedValue = (T)Convert.ChangeType(trimmedElement, typeof(T));
                    result.Add(convertedValue);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CsvUtility.GetList] Could not parse element '{element}' to type '{typeof(T).Name}'. Skipping. Error: {ex.Message}");
                }
            }

            return result;
        }
        
        /// <summary>
        /// A helper that converts a string value to a target type, with special handling for common types.
        /// </summary>
        private static object ConvertValue(string stringValue, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            // Handle Lists
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = targetType.GetGenericArguments()[0];
                var getListMethod = typeof(CsvUtility).GetMethod(nameof(GetList), BindingFlags.Public | BindingFlags.Static);
                if (getListMethod != null)
                {
                    var genericMethod = getListMethod.MakeGenericMethod(itemType);
                    // Assume ';' as default separator for lists
                    return genericMethod.Invoke(null, new object[] { stringValue, ";" });
                }
            }

            // Handle specific primitive types with robust parsing
            if (targetType == typeof(float))
            {
                return TryGetFloat(stringValue, out var f) ? f : 0;
            }
            if (targetType == typeof(double))
            {
                return TryGetDouble(stringValue, out var d) ? d : 0;
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromInvariantString(stringValue);
            }

            // Fallback if no TypeConverter exists
            return Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}