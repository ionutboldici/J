using System;
using System.Windows.Forms;
using System.Drawing;
using J100.Services;
using J100.OutputProcessors;
using J100.Components;

namespace J100
{
    public partial class Form1 : Form
    {
        private LogFilterService _logFilterService;
        private ConfigReader _configReader;
        private ICTOutputProcessor _outputProcessor;

        private ChenarCaleLoguri _chenarCaleLoguri;
        private ChenarCaleRaport _chenarCaleRaport;
        private ChenarTipRaport _chenarTipRaport;
        private ChenarPerioada _chenarPerioada;
        private ChenarFiltre _chenarFiltre;
        private ChenarStatistici _chenarStatistici;
        private ChenarConsola _chenarConsola;
        private ButonStart _butonStart;

        public static ChenarCaleLoguri ChenarCaleLoguriInstance { get; private set; }
        public static ChenarPerioada ChenarPerioadaInstance { get; private set; }

        public Form1()
        {
            InitializeDependencies();
            InitializeComponents();
            SubscribeToEvents();
            SynchronizeComponents();
            ValidateStartButtonState();
        }

        private void InitializeDependencies()
        {
            _configReader = new ConfigReader("config.ini");
            _logFilterService = new LogFilterService(_configReader);

            string outputPath = _configReader.GetValue("Paths", "ReportPath", Environment.CurrentDirectory);
            _outputProcessor = new ICTOutputProcessor(outputPath, _logFilterService, _configReader);
        }

        private void InitializeComponents()
        {
            try
            {
                _chenarConsola = ChenarConsola.Instance;

                this.Text = "J-100 Log Extractor v1.0";
                this.ClientSize = new System.Drawing.Size(1200, 660);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.Padding = new Padding(10);

                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 6,
                    Padding = new Padding(5)
                };

                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.StartButtonHeight + 10));

                string compatibleFiles = _configReader.GetValue("General", "FisiereCompatibile", "txt,zip,tmw");

                _chenarCaleLoguri = new ChenarCaleLoguri(_chenarConsola, _configReader, compatibleFiles, _logFilterService)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _chenarCaleRaport = new ChenarCaleRaport(_configReader, _chenarConsola, _logFilterService)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _chenarTipRaport = new ChenarTipRaport(_logFilterService, _configReader)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _chenarPerioada = new ChenarPerioada(_logFilterService, _configReader)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _chenarFiltre = new ChenarFiltre(_logFilterService, _configReader)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _chenarStatistici = new ChenarStatistici(_logFilterService, _chenarCaleLoguri)
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                _butonStart = new ButonStart(_logFilterService, _outputProcessor)
                {
                    Width = UIConfig.StartButtonWidth,
                    Height = UIConfig.StartButtonHeight,
                    Font = UIConfig.DefaultFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.None
                };

                mainLayout.Controls.Add(_chenarCaleLoguri, 0, 0);
                mainLayout.Controls.Add(_chenarFiltre, 1, 0);
                mainLayout.SetRowSpan(_chenarFiltre, 2);
                mainLayout.Controls.Add(_chenarCaleRaport, 0, 1);
                mainLayout.Controls.Add(_chenarTipRaport, 0, 2);
                mainLayout.Controls.Add(_chenarPerioada, 0, 3);
                mainLayout.Controls.Add(_chenarStatistici, 1, 2);
                mainLayout.SetRowSpan(_chenarStatistici, 2);
                mainLayout.Controls.Add(_butonStart, 0, 5);
                mainLayout.SetColumnSpan(_butonStart, 2);

                _chenarConsola.Dock = DockStyle.Fill;
                mainLayout.Controls.Add(_chenarConsola, 0, 4);
                mainLayout.SetColumnSpan(_chenarConsola, 2);

                ChenarCaleLoguriInstance = _chenarCaleLoguri;
                ChenarPerioadaInstance = _chenarPerioada;

                this.Controls.Add(mainLayout);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eroare la inițializarea aplicației: {ex.Message}", "Eroare Inițializare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SubscribeToEvents()
        {
            _chenarTipRaport.ReportTypeChanged += (s, e) =>
            {
                _chenarPerioada.UpdateFieldsBasedOnReportType();
                _chenarCaleLoguri.UpdateReportType(_logFilterService.ReportType, _logFilterService.PeriodStart, _logFilterService.PeriodEnd);
                _chenarFiltre.LoadInitialFilters();
                ValidateStartButtonState();
            };

            _chenarCaleLoguri.LogPathChanged += (s, e) =>
            {
                ValidateStartButtonState();
            };

            _chenarFiltre.FiltersChanged += (s, e) =>
            {
                ValidateStartButtonState();
            };

            if (_chenarCaleRaport is ChenarCaleRaport chenarRaport && chenarRaport.GetType().GetEvent("ReportPathChanged") != null)
            {
                chenarRaport.ReportPathChanged += (s, e) =>
                {
                    ValidateStartButtonState();
                };
            }
        }

        private void SynchronizeComponents()
        {
            _chenarPerioada.UpdateFieldsBasedOnReportType();
            _chenarCaleLoguri.UpdateReportType(_logFilterService.ReportType, _logFilterService.PeriodStart, _logFilterService.PeriodEnd);
        }

        private void ValidateStartButtonState()
        {
            if (_logFilterService.ValidateConfiguration(out var errors))
            {
                _butonStart.Enabled = true;
                _chenarConsola.WriteMessage("[INFO]: Configurația este validă. Butonul START este activat.");
            }
            else
            {
                _butonStart.Enabled = false;
                _chenarConsola.WriteMessage($"[WARNING]: Configurația este invalidă. Erori: {string.Join(", ", errors)}");
            }
        }
    }
}
