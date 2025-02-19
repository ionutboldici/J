using System;
using System.Collections.Generic;
using System.IO;
using J100.Services;
using OfficeOpenXml;

namespace J100.OutputProcessors
{
    public class Custom
    {
        private readonly string _outputPath;
        private readonly LogFilterService _logFilterService;

        public Custom(string outputPath, LogFilterService logFilterService)
        {
            _outputPath = outputPath;
            _logFilterService = logFilterService;
        }

        public void GenerateReport(List<string> logFiles)
        {
            try
            {
                if (logFiles == null || logFiles.Count == 0)
                {
                    Console.WriteLine("[ERROR]: Nu există fișiere de log pentru a genera raportul Custom.");
                    return;
                }

                string outputFileName = $"Custom_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
                string fullOutputPath = Path.Combine(_outputPath, outputFileName);

                using (var package = new ExcelPackage(new FileInfo(fullOutputPath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("RaportCustom");
                    int row = 1;

                    // Adăugăm antetul tabelului
                    worksheet.Cells[row, 1].Value = "Nume Fișier";
                    worksheet.Cells[row, 2].Value = "Data Creării";
                    worksheet.Cells[row, 3].Value = "Linie START";
                    worksheet.Cells[row, 4].Value = "Linie LOT";
                    row++;

                    // Iterăm prin fiecare fișier de log și extragem datele necesare prin callback
                    foreach (var file in logFiles)
                    {
                        string startLine = _logFilterService.HandleCallback("START", file);
                        string lotLine = _logFilterService.HandleCallback("LOT", file);
                        string fileName = _logFilterService.HandleCallback("FileName", file);
                        string creationDate = _logFilterService.HandleCallback("CreationDate", file);

                        worksheet.Cells[row, 1].Value = fileName;
                        worksheet.Cells[row, 2].Value = creationDate;
                        worksheet.Cells[row, 3].Value = startLine;
                        worksheet.Cells[row, 4].Value = lotLine;

                        row++;
                    }

                    package.Save();
                }

                Console.WriteLine($"[INFO]: Raportul Custom a fost salvat în: {fullOutputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: Eroare la generarea raportului Custom: {ex.Message}");
            }
        }
    }
}
