using System;
using System.Drawing;
using System.Windows.Forms;
using J100.Services;
using J100.OutputProcessors;

namespace J100.Components
{
    public class ButonStart : Button
    {
        private readonly LogFilterService _logFilterService;
        private readonly ICTOutputProcessor _outputProcessor;
        private bool _isProcessing;

        public ButonStart(LogFilterService logFilterService, ICTOutputProcessor outputProcessor)
        {
            _logFilterService = logFilterService ?? throw new ArgumentNullException(nameof(logFilterService));
            _outputProcessor = outputProcessor ?? throw new ArgumentNullException(nameof(outputProcessor));

            InitializeButton();
            UpdateButtonState(false); // Inițial, butonul este dezactivat

            SubscribeToEvents();
        }

        private void InitializeButton()
        {
            this.Text = "START";
            this.Width = UIConfig.StartButtonWidth;
            this.Height = UIConfig.StartButtonHeight;
            this.Font = UIConfig.DefaultFont;
            this.FlatStyle = FlatStyle.Flat;
            this.TextAlign = ContentAlignment.MiddleCenter;
            this.Click += OnButtonClick;
        }

        private void SubscribeToEvents()
        {
            _logFilterService.ProgressUpdated += (processed, total) =>
            {
                if (_isProcessing)
                {
                    this.Text = $"STOP ({processed}/{total})";
                }
            };

            ChenarConsola.Instance?.WriteMessage("ButonStart: Evenimentele au fost configurate corect.", "DEBUG");
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                StopProcessing();
            }
            else
            {
                StartProcessing();
            }
        }

        private void StartProcessing()
        {
            try
            {
                if (!_logFilterService.ValidateConfiguration(out var errors))
                {
                    ChenarConsola.Instance?.WriteMessage($"[ERROR]: Configurația este invalidă: {string.Join(", ", errors)}", "ERROR");
                    return;
                }

                _isProcessing = true;
                UpdateButtonState(true);

                ChenarConsola.Instance?.WriteMessage("Procesarea fișierelor a început.", "INFO");

                _logFilterService.ProcessLogs();
                _outputProcessor.GenerateReport();

                ChenarConsola.Instance?.WriteMessage("Procesarea fișierelor s-a încheiat.", "SUCCESS");
            }
            catch (Exception ex)
            {
                ChenarConsola.Instance?.WriteMessage($"[ERROR]: Eroare în timpul procesării: {ex.Message}", "ERROR");
            }
            finally
            {
                StopProcessing();
            }
        }

        private void StopProcessing()
        {
            _isProcessing = false;
            UpdateButtonState(false);

            ChenarConsola.Instance?.WriteMessage("Procesarea a fost oprită de utilizator.", "WARNING");
        }

        private void UpdateButtonState(bool isProcessing)
        {
            if (isProcessing)
            {
                this.Text = "STOP";
                this.BackColor = Color.Red;
                this.Enabled = true;
            }
            else
            {
                bool isReady = _logFilterService.IsReadyForProcessing();
                this.Text = isReady ? "START" : "START";
                this.BackColor = isReady ? Color.Green : Color.LightGray;
                this.Enabled = isReady;

                ChenarConsola.Instance?.WriteMessage($"[DEBUG]: IsReadyForProcessing = {isReady}, Buton activat: {this.Enabled}");
            }
        }

        // Verifică și setează stările butonului conform set4
        public void ApplySet4State(string state, int processed = 0, int total = 0)
        {
            switch (state.ToLower())
            {
                case "inactive":
                    this.Text = "START";
                    this.BackColor = Color.LightGray;
                    this.Enabled = false;
                    break;

                case "ready":
                    this.Text = "START";
                    this.BackColor = Color.Green;
                    this.Enabled = true;
                    break;

                case "processing":
                    this.Text = "STOP";
                    this.BackColor = Color.Red;
                    this.Enabled = true;
                    break;

                case "progress":
                    this.Text = $"STOP ({processed}/{total})";
                    this.BackColor = Color.Red;
                    this.Enabled = true;
                    break;

                default:
                    ChenarConsola.Instance?.WriteMessage($"[WARNING]: Stare necunoscută pentru buton: {state}", "WARNING");
                    break;
            }
        }
    }
}
