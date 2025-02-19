using J100.Services;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace J100.Components
{
    public class ChenarStatistici : Panel
    {
        private Label _labelTitle;
        private Label _labelTotalFiles;
        private Label _labelProcessedFiles;
        private Label _labelErrors;
        private Label _labelProgress;
        private ProgressBar _progressBar;

        private LogFilterService _logFilterService;
        private ChenarCaleLoguri _chenarCaleLoguri;

        // Constructor principal
        public ChenarStatistici()
        {
            InitializeComponents();
        }

        public ChenarStatistici(LogFilterService logFilterService, ChenarCaleLoguri chenarCaleLoguri)
        {
            _logFilterService = logFilterService;
            _chenarCaleLoguri = chenarCaleLoguri;
            InitializeComponents();
            SubscribeToEvents();
        }

        // Inițializarea componentelor GUI
        private void InitializeComponents()
        {
            // Configurarea panel-ului
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Size = new System.Drawing.Size(460, 150);

            // Titlu
            _labelTitle = new Label
            {
                Text = "Statistici Procesare:",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(_labelTitle);

            // Etichete și valori pentru statistici
            _labelTotalFiles = CreateLabel("Total fișiere: 0", 40);
            _labelProcessedFiles = CreateLabel("Fișiere procesate: 0", 60);
            _labelErrors = CreateLabel("Eșecuri: 0", 80);

            this.Controls.Add(_labelTotalFiles);
            this.Controls.Add(_labelProcessedFiles);
            this.Controls.Add(_labelErrors);

            // Etichetă progres
            _labelProgress = new Label
            {
                Text = "Progres:",
                Location = new System.Drawing.Point(10, 100),
                Size = new System.Drawing.Size(60, 20)
            };
            this.Controls.Add(_labelProgress);

            // Bara de progres
            _progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(80, 100),
                Size = new System.Drawing.Size(360, 20)
            };
            this.Controls.Add(_progressBar);
        }

        // Creare etichetă pentru statistici
        private Label CreateLabel(string text, int yPosition)
        {
            return new Label
            {
                Text = text,
                Location = new System.Drawing.Point(10, yPosition),
                Size = new System.Drawing.Size(300, 20)
            };
        }

        // Metodă pentru actualizarea statisticilor
        public void UpdateStatistics(int totalFiles, int processedFiles, int errors)
        {
            _labelTotalFiles.Text = $"Total fișiere: {totalFiles}";
            _labelProcessedFiles.Text = $"Fișiere procesate: {processedFiles}";
            _labelErrors.Text = $"Eșecuri: {errors}";

            // Calcul progres
            int progress = totalFiles > 0 ? (processedFiles * 100 / totalFiles) : 0;
            _progressBar.Value = progress;

            // Informare consolă
           // ChenarConsola.Instance?.WriteMessage($"[INFO]: Actualizare statistici - Total: {totalFiles}, Procesate: {processedFiles}, Eșecuri: {errors}, Progres: {progress}%.", "INFO");

            if (errors > 0)
            {
                ChenarConsola.Instance?.WriteMessage("[WARNING]: Procesarea a generat erori. Verificați logurile pentru detalii.", "WARNING");
            }
        }

        // Abonare la evenimentele LogFilterService și ChenarCaleLoguri
        private void SubscribeToEvents()
        {
            if (_logFilterService != null)
            {
                _logFilterService.ProgressUpdated += OnProgressUpdated;
                _logFilterService.ErrorOccurred += OnErrorOccurred;
            }

            if (_chenarCaleLoguri != null)
            {
                // Afișăm `NTotal` din ChenarCaleLoguri când acesta este recalculat
                _chenarCaleLoguri.NTotalChanged += OnNTotalChanged;
            }
        }

        // Gestionarea evenimentului de actualizare progres
        private void OnProgressUpdated(int processedFiles, int totalFiles)
        {
            int errors = totalFiles - processedFiles; // Calculăm fișierele eșuate
            UpdateStatistics(totalFiles, processedFiles, errors);
        }

        // Gestionarea evenimentului de eroare
        private void OnErrorOccurred(string errorMessage)
        {
            ChenarConsola.Instance?.WriteMessage($"[ERROR]: {errorMessage}", "ERROR");
        }

        // Gestionarea modificării lui NTotal
        private void OnNTotalChanged(object sender, EventArgs e)
        {
            if (_chenarCaleLoguri != null)
            {
                int nTotal = _chenarCaleLoguri.NTotal;
                UpdateStatistics(nTotal, 0, 0); // Resetează progresul la schimbarea totalului
                ChenarConsola.Instance?.WriteMessage($"[INFO]: NTotal actualizat: {nTotal} fișiere compatibile.", "INFO");
            }
        }
    }
}
