using J100.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace J100.Services
{
    public class ConfigReader
    {
        private readonly string _configPath;
        private readonly Dictionary<string, Dictionary<string, string>> _configData;
        private readonly List<string> _originalLines;

        public ConfigReader(string configPath)
        {
            _configPath = configPath;
            _configData = new Dictionary<string, Dictionary<string, string>>();
            _originalLines = new List<string>();
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    throw new FileNotFoundException("Fișierul config.ini nu a fost găsit.");
                }

                _originalLines.Clear();
                _configData.Clear();
                string currentSection = string.Empty;

                foreach (var line in File.ReadLines(_configPath))
                {
                    _originalLines.Add(line);
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Trim('[', ']');
                        if (!_configData.ContainsKey(currentSection))
                        {
                            _configData[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    else if (currentSection != string.Empty)
                    {
                        var keyValue = trimmedLine.Split(new[] { '=' }, 2, StringSplitOptions.None);
                        if (keyValue.Length == 2)
                        {
                            var value = keyValue[1].Split('#')[0].Trim();
                            _configData[currentSection][keyValue[0].Trim()] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChenarConsola.Instance.WriteError($"Eroare la încărcarea configurației: {ex.Message}");
                erl.LogError($"[ConfigReader] {ex.Message}");
            }
        }

        public string GetValue(string section, string key, string defaultValue = null)
        {
            if (_configData.ContainsKey(section) && _configData[section].ContainsKey(key))
            {
                return _configData[section][key];
            }
            return defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!ValidateValue(section, key, value))
            {
                string errorMessage = $"Valoare invalidă pentru {key} în secțiunea {section}.";
                ChenarConsola.Instance.WriteWarning(errorMessage);
                erl.LogWarning($"[ConfigReader] {errorMessage}");
                return;
            }

            if (!_configData.ContainsKey(section))
            {
                _configData[section] = new Dictionary<string, string>();
            }
            _configData[section][key] = value;
        }

        // Adăugăm metode pentru gestionarea CDMCValue
        public string GetCDMCValue()
        {
            return GetValue("ReportTypes", "CDMCValue", "");
        }

        public void SetCDMCValue(string value)
        {
            SetValue("ReportTypes", "CDMCValue", value);
            SaveConfig();
        }

        public Dictionary<string, string> GetFilters(string reportType)
        {
            var filters = new Dictionary<string, string>();
            var fullType = NormalizeReportType(reportType);

            for (int i = 1; i <= 4; i++)
            {
                string key = $"Filter{i}{fullType}";
                string filterValue = GetValue("Filters", key, string.Empty);
                if (!string.IsNullOrWhiteSpace(filterValue))
                {
                    filters[key] = filterValue;
                }
            }

            return filters;
        }

        private bool ValidateValue(string section, string key, string value)
        {
            if (section == "General")
            {
                if (key == "autostart" && !(value == "Y" || value == "N")) return false;
            }
            if (section == "Paths" && string.IsNullOrWhiteSpace(value)) return false;
            if (section == "OutputFormats" && !(value == "true" || value == "false")) return false;
            if (section == "Period")
            {
                if ((key == "StartDate" || key == "EndDate") && !DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    return false;
                if ((key == "StartTime" || key == "EndTime") && !DateTime.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    return false;
            }
            if (section == "ReportTypes") return NormalizeReportType(value) != "ICT";
            return true;
        }

        private string NormalizeReportType(string reportType)
        {
            reportType = reportType.ToUpper();

            if (reportType == "I" || reportType == "ICT") return "ICT";
            if (reportType == "E" || reportType == "EOL") return "EOL";
            if (reportType == "C" || reportType == "CUSTOM") return "Custom";
            if (reportType == "CON" || reportType == "CONCATENARE") return "Concatenare";
            if (reportType == "CDMC") return "CDMC";

            return "ICT";
        }

        public void SaveConfig()
        {
            try
            {
                var newLines = new List<string>();
                string currentSection = string.Empty;

                foreach (var line in _originalLines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        newLines.Add(line);
                        continue;
                    }

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Trim('[', ']');
                        newLines.Add(line);
                        continue;
                    }

                    var keyValue = trimmedLine.Split(new[] { '=' }, 2, StringSplitOptions.None);
                    if (keyValue.Length == 2 && _configData.ContainsKey(currentSection) && _configData[currentSection].ContainsKey(keyValue[0].Trim()))
                    {
                        string newValue = _configData[currentSection][keyValue[0].Trim()];
                        newLines.Add($"{keyValue[0].Trim()}={newValue}");
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                File.WriteAllLines(_configPath, newLines);
                ChenarConsola.Instance.WriteMessage("Configurația a fost salvată cu succes.", "INFO");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Eroare la salvarea configurației: {ex.Message}";
                ChenarConsola.Instance.WriteError(errorMessage);
                erl.LogError($"[ConfigReader] {ex.Message}");
            }
        }
    }
}
