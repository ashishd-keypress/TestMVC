using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Utility
{
    public static class Helper
    {
        private static readonly object _lockObj = new object();

        /// <summary>
        /// Retrieves the logged in UserId integer from ClaimsPrincipal if authenticated.
        /// </summary>
        public static int? GetUserId(this System.Security.Claims.ClaimsPrincipal? principal)
        {
            if (principal == null) return null;
            var claim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int uid) && uid > 0)
            {
                return uid;
            }
            return null;
        }

        /// <summary>
        /// Writes structured log message to a file under the specified relative path and file name.
        /// </summary>
        public static void WriteToFile(string message, string relativePath, string fileName, bool isError = false)
        {
            try
            {
                string baseLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string cleanRelativePath = relativePath.TrimStart('\\', '/');
                string targetDir = Path.Combine(baseLogDir, cleanRelativePath);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Sanitize file name
                string safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(targetDir, safeFileName + ".txt");

                lock (_lockObj)
                {
                    File.AppendAllText(filePath, message + Environment.NewLine);
                }
            }
            catch
            {
                // Silently prevent logging failures from breaking request execution
            }
        }

        /// <summary>
        /// Maps an IDataReader / SqlDataReader to a generic List of objects using property reflection.
        /// </summary>
        public static List<T> DataReaderMapToList<T>(this IDataReader dr) where T : new()
        {
            var list = new List<T>();
            if (dr == null) return list;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanWrite)
                                      .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            // Collect column names present in the data reader
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dr.FieldCount; i++)
            {
                columnNames.Add(dr.GetName(i));
            }

            while (dr.Read())
            {
                var obj = new T();
                foreach (var colName in columnNames)
                {
                    if (properties.TryGetValue(colName, out var prop))
                    {
                        var val = dr[colName];
                        if (val != null && val != DBNull.Value)
                        {
                            try
                            {
                                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                if (targetType.IsEnum)
                                {
                                    prop.SetValue(obj, Enum.ToObject(targetType, val));
                                }
                                else
                                {
                                    var convertedVal = Convert.ChangeType(val, targetType);
                                    prop.SetValue(obj, convertedVal);
                                }
                            }
                            catch
                            {
                                // Fallback for specific type conversions if any
                            }
                        }
                    }
                }
                list.Add(obj);
            }

            return list;
        }

        /// <summary>
        /// Maps the first record of an IDataReader to a single object or default.
        /// </summary>
        public static T? DataReaderMapToSingle<T>(this IDataReader dr) where T : new()
        {
            var list = dr.DataReaderMapToList<T>();
            return list.FirstOrDefault();
        }
    }
}
