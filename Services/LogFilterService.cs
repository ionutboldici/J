using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using J100.OutputProcessors;

namespace J100.Services
{
    public class LogFilterService
    {
        // ========================================
        // 🔹 1. EVENIMENTE PENTRU NOTIFICĂRI ȘI PROGRES
        // ========================================
        public event Action<int, int> ProgressUpdated;
        public event Action<string> FileProcessed;
        public event Action<string> ErrorOccurred;

        // ========================================
        // 🔹 2. PROPRIETĂȚI PENTRU GESTIONAREA CONFIGURĂRILOR
        // ========================================
        public string LogPath { get; private set; }
        public string ReportPath { get; private set; }
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        private string _reportType;
        public string ReportType
        {
            get => _reportType;
            set
            {
                if (_reportType != value)
                {
                    _reportType = value;
                    NotifyReportTypeChanged();
                    SyncFiltersWithReportType();
                }
            }
        }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public bool IsProcessing { get; private set; } = false;

        public event EventHandler ReportTypeChanged;

        // ========================================
        // 🔹 3. INIȚIALIZARE SERVICIU ȘI CONFIGURARE
        // ========================================
        private readonly ConfigReader _configReader;
        private bool _stopProcessing;

        public LogFilterService(ConfigReader configReader)
        {
            _configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            LogPath = _configReader.GetValue("Paths", "LogPath", string.Empty);
            ReportPath = _configReader.GetValue("Paths", "ReportPath", string.Empty);

            Console.WriteLine($"[DEBUG]: LogPath inițializat: {LogPath}");
            Console.WriteLine($"[DEBUG]: ReportPath inițializat: {ReportPath}");
        }

        // ========================================
        // 🔹 4. GESTIONAREA TIPULUI DE RAPORT
        // ========================================
        private void SyncFiltersWithReportType()
        {
            if (string.IsNullOrWhiteSpace(ReportType))
            {
                LogError("Tipul raportului este invalid sau nespecificat.");
                ErrorOccurred?.Invoke("Tipul raportului este invalid sau nespecificat.");
                return;
            }

            try
            {
                Filters = _configReader.GetFilters(ReportType);
                Console.WriteLine($"[INFO]: Filtrele au fost sincronizate pentru tipul de raport '{ReportType}'.");
            }
            catch (Exception ex)
            {
                LogError($"Eroare la sincronizarea filtrelor: {ex.Message}");
                ErrorOccurred?.Invoke($"Eroare la sincronizarea filtrelor: {ex.Message}");
            }
        }

        private void NotifyReportTypeChanged()
        {
            ReportTypeChanged?.Invoke(this, EventArgs.Empty);
        }

        // ========================================
        // 🔹 5. GESTIONAREA CĂILOR LOGURILOR ȘI RAPOARTELOR
        // ========================================
        public void UpdateLogPath(string newLogPath)
        {
            if (string.IsNullOrWhiteSpace(newLogPath) || !Directory.Exists(newLogPath))
            {
                LogError("Calea logurilor specificată este invalidă.");
                ErrorOccurred?.Invoke("Calea logurilor specificată este invalidă.");
                return;
            }

            LogPath = newLogPath;
            Console.WriteLine($"[INFO]: LogPath a fost actualizat: {LogPath}");
        }

        public void UpdateReportPath(string newReportPath)
        {
            if (string.IsNullOrWhiteSpace(newReportPath) || !Directory.Exists(newReportPath))
            {
                LogError("Calea rapoartelor specificată este invalidă.");
                ErrorOccurred?.Invoke("Calea rapoartelor specificată este invalidă.");
                return;
            }

            ReportPath = newReportPath;
            Console.WriteLine($"[INFO]: ReportPath a fost actualizat: {ReportPath}");
        }

        public void UpdateCDMCValue(string cdmcValue)
        {
            if (string.IsNullOrWhiteSpace(cdmcValue))
            {
                Console.WriteLine("[WARNING]: Valoarea CDMC este goală.");
                return;
            }

            // Salvăm valoarea CDMC în fișierul de configurare
            _configReader.SetValue("ReportTypes", "CDMCValue", cdmcValue);
            _configReader.SaveConfig();

            // Afișăm mesaj informativ în consolă
            Console.WriteLine($"[INFO]: Valoarea CDMC a fost actualizată la: {cdmcValue}");
        }

        // ========================================
        // 🔹 6. VALIDAREA CONFIGURAȚIEI
        // ========================================
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LogPath) || !Directory.Exists(LogPath))
                errors.Add("Calea logurilor este invalidă sau inexistentă.");

            if (string.IsNullOrWhiteSpace(ReportPath) || !Directory.Exists(ReportPath))
                errors.Add("Calea rapoartelor este invalidă sau inexistentă.");

            if (Filters == null || Filters.Count == 0)
                errors.Add("Nu există filtre configurate.");

            if (string.IsNullOrWhiteSpace(ReportType))
                errors.Add("Tipul raportului nu este specificat.");

            if (PeriodStart >= PeriodEnd)
                errors.Add("Perioada selectată este invalidă. Data de început trebuie să fie mai mică decât data de sfârșit.");

            return errors.Count == 0;
        }

        public bool IsReadyForProcessing()
        {
            return ValidateConfiguration(out _);
        }

        // ========================================
        // 🔹 7. PROCESAREA LOGURILOR
        // ========================================
        public void ProcessLogs()
        {
            if (IsProcessing)
            {
                LogError("Procesarea este deja în desfășurare.");
                ErrorOccurred?.Invoke("Procesarea este deja în desfășurare.");
                return;
            }

            IsProcessing = true;
            _stopProcessing = false;

            try
            {
                var compatibleExtensions = _configReader.GetValue("General", "FisiereCompatibile", string.Empty)
                                         .Split(',')
                                         .Select(ext => ext.Trim().ToLower())
                                         .ToArray();

                if (!compatibleExtensions.Any())
                {
                    LogError("Lipsă extensii compatibile în configurație. Verificați FisiereCompatibile din config.ini.");
                    ErrorOccurred?.Invoke("Lipsă extensii compatibile în configurație. Verificați FisiereCompatibile din config.ini.");
                    IsProcessing = false;
                    return;
                }

                var logFiles = Directory.GetFiles(LogPath, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(file => compatibleExtensions.Contains(Path.GetExtension(file).ToLower()))
                                        .ToList();

                if (!logFiles.Any())
                {
                    LogError("Nu există fișiere compatibile pentru procesare.");
                    ErrorOccurred?.Invoke("Nu există fișiere compatibile pentru procesare.");
                    IsProcessing = false;
                    return;
                }

                int totalFiles = logFiles.Count;
                int processedFiles = 0;

                foreach (var file in logFiles)
                {
                    if (_stopProcessing)
                    {
                        LogError("Procesarea a fost oprită de utilizator.");
                        ErrorOccurred?.Invoke("Procesarea a fost oprită de utilizator.");
                        break;
                    }

                    try
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            if (ApplyFilters(line) && IsWithinPeriod(file))
                            {
                                FileProcessed?.Invoke(file);
                            }
                        }

                        processedFiles++;
                        ProgressUpdated?.Invoke(processedFiles, totalFiles);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Eroare la procesarea fișierului {file}: {ex.Message}");
                        ErrorOccurred?.Invoke($"Eroare la procesarea fișierului {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Eroare globală la procesarea logurilor: {ex.Message}");
                ErrorOccurred?.Invoke($"Eroare globală la procesarea logurilor: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Oprește procesarea logurilor în desfășurare.
        /// </summary>
        public void StopProcessing()
        {
            if (!IsProcessing)
            {
                LogError("Nu există niciun proces în desfășurare care să fie oprit.");
                ErrorOccurred?.Invoke("Nu există niciun proces în desfășurare care să fie oprit.");
                return;
            }

            _stopProcessing = true;
        }

        // ========================================
        // 🔹 8. FUNCȚII UTILITARE ȘI HANDLING ERORI
        // ========================================
        /// <summary>
        /// Aplică filtrele definite asupra unei linii din log.
        /// </summary>
        /// <param name="line">Linia citită din fișierul log</param>
        /// <returns>True dacă linia trece filtrarea, False altfel</returns>
        private bool ApplyFilters(string line)
        {
            foreach (var filter in Filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Value)) continue;

                if (filter.Key.StartsWith("Filter1") && !line.StartsWith(filter.Value))
                {
                    return false;
                }

                if (filter.Key.StartsWith("Filter2") && !line.Contains(filter.Value))
                {
                    return false;
                }

                if (filter.Key.StartsWith("Filter3") && !line.Split(' ').Contains(filter.Value))
                {
                    return false;
                }

                if (filter.Key.StartsWith("Filter4") && line.Contains(filter.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifică dacă un fișier log este în perioada selectată.
        /// </summary>
        /// <param name="filePath">Calea fișierului</param>
        /// <returns>True dacă fișierul este în perioada selectată, False altfel</returns>
        private bool IsWithinPeriod(string filePath)
        {
            try
            {
                var creationTime = File.GetCreationTime(filePath);
                return creationTime >= PeriodStart && creationTime <= PeriodEnd;
            }
            catch
            {
                return false;
            }
        }

                /// <summary>
        /// Procesează callback-urile pentru diferite tipuri de date extrase din loguri.
        /// </summary>
        /// <param name="callbackType">Tipul de callback</param>
        /// <param name="filePath">Calea fișierului log</param>
        /// <returns>Informația extrasă în funcție de tipul de callback</returns>
        public string HandleCallback(string callbackType, string filePath)
        {
            try
            {
                if (callbackType == "START")
                {
                    return GetLineStartingWith(filePath, "START");
                }
                else if (callbackType == "LOT")
                {
                    return GetLineStartingWith(filePath, "LOT");
                }
                else if (callbackType == "FileName")
                {
                    return Path.GetFileName(filePath);
                }
                else if (callbackType == "CreationDate")
                {
                    return File.GetCreationTime(filePath).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    throw new ArgumentException("Tip de callback necunoscut.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Eroare la procesarea callback-ului \"{callbackType}\": {ex.Message}");
                ErrorOccurred?.Invoke($"Eroare la procesarea callback-ului \"{callbackType}\": {ex.Message}");
                return null;
            }
        }

        private string GetLineStartingWith(string filePath, string prefix)
        {
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    if (line.StartsWith(prefix))
                    {
                        return line;
                    }
                }
                return $"Linia care începe cu \"{prefix}\" nu a fost găsită.";
            }
            catch (Exception ex)
            {
                LogError($"Eroare la citirea liniei din fișier \"{filePath}\": {ex.Message}");
                ErrorOccurred?.Invoke($"Eroare la citirea liniei din fișier \"{filePath}\": {ex.Message}");
                return null;
            }
        }

        public void UpdateFilters(Dictionary<string, string> newFilters)
        {
            Filters = newFilters;
            Console.WriteLine("[INFO]: Filtrele au fost actualizate manual în LogFilterService.");
        }

        private void LogError(string message)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logFilePath, $"[{timestamp}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Dacă apare o eroare la logare, aceasta nu este tratată suplimentar.
            }
        }
    }
}
