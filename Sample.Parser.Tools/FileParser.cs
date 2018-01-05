using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Sample.Parser.Tools
{
    public class FileParser<T> where T : new()
    {
        private readonly Regex _wordRegexCommo = new Regex("((?<=\")[^\"]*(?=\"(,|$)+)|(?<=,|^)[^,\"]*(?=,|$))", RegexOptions.Compiled);
        private readonly Regex _wordRegexSeparatorPoint = new Regex("((?<=\")[^\"]*(?=\"(;|$)+)|(?<=;|^)[^;\"]*(?=;|$))", RegexOptions.Compiled);


        private List<PropertyInfo> _properties;
        private Dictionary<string, string> _mappingExcelColumnsToObject;
        private Dictionary<string, string> _mappingObjectToExcelColumns;

        private Regex _wordRegex
        {
            get
            {
                switch (_separator)
                {
                    case ',':
                        return _wordRegexCommo;
                    case ';':
                        return _wordRegexSeparatorPoint;
                    default:
                        throw new InvalidParserFormatException("Error: Regex Separator not available");
                }
            }
        }

        private char _separator { get; set; }

        private void InitMapping()
        {
            Dictionary<string, string> dicMapping = new Dictionary<string, string>();
            var type = typeof(T);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.GetProperty | BindingFlags.SetProperty);

            var q = properties.Where(x => x.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0).AsQueryable();

            _properties = q.OrderBy(a => a.Name).ToList();

            _mappingExcelColumnsToObject = new Dictionary<string, string>();
            _mappingObjectToExcelColumns = new Dictionary<string, string>();

            foreach (var propertyInfo in _properties.Where(x => x.GetCustomAttributes(typeof(MappingColAttribute), false).Length > 0))
            {
                var mappingColName = (MappingColAttribute)propertyInfo.GetCustomAttributes(typeof(MappingColAttribute), false).FirstOrDefault();

                if (mappingColName != null)
                {
                    _mappingExcelColumnsToObject.Add(mappingColName.ColName, propertyInfo.Name);
                    _mappingObjectToExcelColumns.Add(propertyInfo.Name, mappingColName.ColName);
                }
            }
        }

        public FileParser(char separator)
        {
            _separator = separator;
            InitMapping();
        }

        public void Serialize(Stream stream, IList<T> data)
        {
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(BuildString(data).ToString().Trim());
            }
        }

        public string Serialize(IList<T> data)
        {
            return BuildString(data).ToString().Trim();
        }

        public IList<T> Deserialize(Stream stream)
        {
            //Mapping

            string[] columns;
            string[] rows;

            try
            {
                using (var sr = new StreamReader(stream))
                {
                    // Get columns
                    columns = sr.ReadLine().Replace("\"", "").Split(_separator);
                    var content = sr.ReadToEnd();
                    var lineReturn = content.Contains(Environment.NewLine) ? Environment.NewLine : "\n";
                    // Get Lines
                    rows = content.Split(new[] { lineReturn }, StringSplitOptions.None);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidParserFormatException("The CSV File is Invalid.", ex);
            }

            var data = new List<T>();

            for (int row = 0; row < rows.Length; row++)
            {
                var line = rows[row];

                // Ignore Empty line
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Replace("\"\"", "");

                MatchCollection matches = _wordRegex.Matches(line);
                var parts = (from object match in matches select match.ToString()).ToList();

                if (parts.Count == 1 && parts[0] != null && parts[0] == "EOF")
                    break;

                var datum = new T();

                for (var i = 0; i < parts.Count; i++)
                {
                    var value = parts[i];
                    var column = columns[i];
                    var columnsName = column;

                    if (_mappingExcelColumnsToObject.Count > 0)
                    {
                        if (_mappingExcelColumnsToObject.ContainsKey(column))
                        {
                            columnsName = _mappingExcelColumnsToObject[column];
                        }
                    }

                    var p = _properties.FirstOrDefault(a => a.Name.Equals(columnsName, StringComparison.InvariantCultureIgnoreCase));

                    if (p == null)
                        continue;

                    p.SetValue(datum, Convert.ChangeType(GetConvertedValue(value, p), p.PropertyType), null);
                }
                data.Add(datum);
            }
            return data;
        }

        #region private methods
        private StringBuilder BuildString(IEnumerable<T> data)
        {
            var sb = new StringBuilder();
            var values = new List<string>();

            sb.AppendLine(GetHeader());

            foreach (var item in data)
            {
                values.Clear();
                values.AddRange(_properties.Select(p => p.GetValue(item, null)).Select(raw => raw == null ? "" : raw.ToString().Replace(_separator.ToString(CultureInfo.InvariantCulture), string.Empty)));
                sb.AppendLine(string.Join(_separator.ToString(CultureInfo.InvariantCulture), values.ToArray()));
            }
            return sb;
        }

        private string GetHeader()
        {
            var columns = _properties.Select(a => a.Name).ToArray();

            if (_mappingObjectToExcelColumns.Count > 0)
            {
                for (int i = 0; i < columns.Count(); i++)
                {
                    if (_mappingObjectToExcelColumns.ContainsKey(columns[i]))
                    {
                        columns[i] = _mappingObjectToExcelColumns[columns[i]];
                    }
                }
            }

            var header = string.Join(_separator.ToString(CultureInfo.InvariantCulture), columns);
            return header;
        }

        private dynamic GetConvertedValue(string value, PropertyInfo propertyInfo)
        {
            var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

            dynamic convertedvalue = value.ToUpper();

            if (value.ToUpper() == "NULL")
            {
                convertedvalue = null;
            }
            else if (propertyInfo.PropertyType == typeof(bool) || propertyInfo.PropertyType == typeof(bool?))
            {
                convertedvalue = value != "0";
            }
            else if (propertyInfo.PropertyType == typeof(int) || propertyInfo.PropertyType == typeof(int?))
            {
                if (string.IsNullOrEmpty(convertedvalue))
                    convertedvalue = 0;
                else
                    convertedvalue = converter.ConvertFrom(value);
            }
            else
            {
                convertedvalue = converter.ConvertFrom(value);
            }

            return convertedvalue;
        }
        #endregion

    }
}
