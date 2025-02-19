using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using J100.Services;
using System.Linq;

namespace J100.OutputProcessors
{
    public class ICTOutputProcessor
    {
        private readonly string _outputPath;
        private readonly List<string[]> _data;
        private readonly LogFilterService _logFilterService;
        private readonly ConfigReader _configReader;

        public ICTOutputProcessor(string outputPath, LogFilterService logFilterService, ConfigReader configReader)
        {
            _outputPath = outputPath;
            _data = new List<string[]>();
            _logFilterService = logFilterService;
            _configReader = configReader;
        }

        public void AddProcessedLine(string mainLine, string filePath)
        {
            try
            {
                // Obținere informații suplimentare prin callback-uri
                string startLine = _logFilterService.HandleCallback("START", filePath);
                string lotLine = _logFilterService.HandleCallback("LOT", filePath);
                string fileName = _logFilterService.HandleCallback("FileName", filePath);
                string creationDateString = _logFilterService.HandleCallback("CreationDate", filePath);

                // Parsează data de creare
                DateTime creationDate = DateTime.Parse(creationDateString);
                string dateFormat = _configReader.GetValue("Format", "DateFormat", "MM/dd/yyyy");

                // Procesare liniile START și LOT
                string[] startFields = startLine?.Split(';');
                string[] lotFields = lotLine?.Split(';');

                string masina = fileName.Length >= 8 ? fileName.Substring(0, 8) : "N/A";
                string dmc = fileName.Length >= 10 ? fileName.Substring(fileName.Length - 10, 6) : "N/A";
                string batch = lotFields?.Length > 1 ? lotFields[1] : "N/A";

                string produs = startFields?.Length > 2 ? startFields[2] : "N/A";
                string varianta = startFields?.Length > 4 ? startFields[4] : "N/A";
                string dataTest = startFields?.Length > 6 ? startFields[6] : "N/A";
                string oraTest = startFields?.Length > 7 ? startFields[7] : "N/A";

                string[] groups = mainLine?.Split(';');

                // Adaugare date în lista pentru procesare
                _data.Add(new[]
                {
                    masina, creationDate.ToString(dateFormat), batch, produs, varianta,
                    dmc, dataTest, oraTest
                }.Concat(groups).ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: Eroare la procesarea liniei: {ex.Message}");
            }
        }

        public void GenerateReport()
        {
            try
            {
                string fileName = $"RaportICT_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
                string fullPath = Path.Combine(_outputPath, fileName);

                using (var package = new ExcelPackage(new FileInfo(fullPath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Raport ICT");

                    // Format antet
                    FormatHeader(worksheet);

                    // Adaugare date
                    for (int i = 0; i < _data.Count; i++)
                    {
                        for (int j = 0; j < _data[i].Length; j++)
                        {
                            worksheet.Cells[i + 2, j + 1].Value = _data[i][j];
                            if (j == 1) // Coloana pentru Data log
                            {
                                worksheet.Cells[i + 2, j + 1].Style.Numberformat.Format = "MM/dd/yyyy";
                            }
                        }
                    }

                    // Salvare fișier
                    package.Save();
                    Console.WriteLine("[INFO]: Raportul ICT a fost generat cu succes.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: Generarea raportului ICT a eșuat: {ex.Message}");
            }
        }

        private void FormatHeader(ExcelWorksheet worksheet)
        {
            string[] headers =
            {
                "Masina", "Data log", "Batch", "Produs", "Varianta", "DMC",
                "Data Test", "Ora Test", "Tip Test", "PCB", "Componenta",
                "Pas Test", "Rezultat", "Descriere", "Categorie", "Status",
                "Măsurat", "Minim", "Maxim", "Unitate", "Puncte Test", "Gardare"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 102));
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            worksheet.Row(1).Height = 20;
            worksheet.View.FreezePanes(2, 1);
        }
    }
}
