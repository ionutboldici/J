using System;
using System.Linq;
using System.Windows.Forms;
using J100.Services;
using System.Drawing;

namespace J100.Components
{
    public class ChenarTipRaport : UserControl
    {
        private RadioButton _radioICT;
        private RadioButton _radioEOL;
        private RadioButton _radioCustom;
        private RadioButton _radioConcatenare;
        private RadioButton _radioCDMC;
        private TextBox _txtCDMC;

        private LogFilterService _logFilterService;
        private ConfigReader _configReader;

        public event EventHandler ReportTypeChanged;

        public string SelectedReportType => GetSelectedReportType();
        public string CDMCValue => _txtCDMC.Enabled ? _txtCDMC.Text : string.Empty;

        public ChenarTipRaport(LogFilterService logFilterService, ConfigReader configReader)
        {
            _logFilterService = logFilterService;
            _configReader = configReader;
            InitializeComponents();
            LoadInitialReportType();
            NotifyConsole("ChenarTipRaport a fost inițializat cu succes.");
        }

        private void InitializeComponents()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };

            var label = new Label
            {
                Text = "Selectați tipul raportului:",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Calibri", 12, FontStyle.Bold)
            };

            layout.Controls.Add(label, 0, 0);

            var radioLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            _radioICT = CreateRadioButton("ICT", "ICT");
            _radioEOL = CreateRadioButton("EOL", "EOL");
            _radioCustom = CreateRadioButton("Custom", "Custom");
            _radioConcatenare = CreateRadioButton("Concatenare", "Concatenare");

            _radioCDMC = CreateRadioButton("DMC", "CDMC");
            _radioCDMC.CheckedChanged += OnCDMCCheckedChanged;

            _txtCDMC = new TextBox
            {
                Enabled = false,
                Width = 110,
                Font = new Font("Calibri", 10),
                Height = 25,
                ForeColor = Color.Gray,
                Text = "0"
            };

            _txtCDMC.GotFocus += (s, e) =>
            {
                if (_txtCDMC.Text == "0")
                {
                    _txtCDMC.Text = "";
                    _txtCDMC.ForeColor = Color.Black;
                }
            };

            _txtCDMC.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtCDMC.Text))
                {
                    _txtCDMC.Text = "0";
                    _txtCDMC.ForeColor = Color.Gray;
                }
            };

            radioLayout.Controls.Add(_radioICT);
            radioLayout.Controls.Add(_radioEOL);
            radioLayout.Controls.Add(_radioCustom);
            radioLayout.Controls.Add(_radioConcatenare);
            radioLayout.Controls.Add(_radioCDMC);
            radioLayout.Controls.Add(_txtCDMC);

            layout.Controls.Add(radioLayout, 0, 1);

            this.Controls.Add(layout);
        }

        private RadioButton CreateRadioButton(string label, string value)
        {
            var radioButton = new RadioButton
            {
                Text = label,
                Tag = value,
                Dock = DockStyle.None,
                AutoSize = true,
                Font = new Font("Calibri", 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            radioButton.CheckedChanged += OnReportTypeChanged;
            return radioButton;
        }

        private void OnCDMCCheckedChanged(object sender, EventArgs e)
        {
            _txtCDMC.Enabled = _radioCDMC.Checked;
            if (!_radioCDMC.Checked)
            {
                _txtCDMC.Text = "0";
                _txtCDMC.ForeColor = Color.Gray;
            }
        }

        private void OnReportTypeChanged(object sender, EventArgs e)
        {
            if (!(sender is RadioButton radioButton) || !radioButton.Checked)
                return;

            var selectedReportType = radioButton.Tag.ToString();
            _logFilterService.ReportType = selectedReportType;
            _configReader.SetValue("ReportTypes", "ReportType", selectedReportType);
            _configReader.SaveConfig();

            NotifyAllComponents(selectedReportType);
            NotifyConsole($"Tipul raportului a fost schimbat la: {selectedReportType}");
        }

        private string GetSelectedReportType()
        {
            if (_radioICT.Checked) return "ICT";
            if (_radioEOL.Checked) return "EOL";
            if (_radioCustom.Checked) return "Custom";
            if (_radioConcatenare.Checked) return "Concatenare";
            if (_radioCDMC.Checked) return "CDMC";
            return "ICT";
        }

        private void LoadInitialReportType()
        {
            var initialReportType = _configReader.GetValue("ReportTypes", "ReportType", "ICT");
            var normalizedType = NormalizeReportType(initialReportType);

            switch (normalizedType)
            {
                case "ICT":
                    _radioICT.Checked = true;
                    break;
                case "EOL":
                    _radioEOL.Checked = true;
                    break;
                case "Custom":
                    _radioCustom.Checked = true;
                    break;
                case "Concatenare":
                    _radioConcatenare.Checked = true;
                    break;
                case "CDMC":
                    _radioCDMC.Checked = true;
                    _txtCDMC.Enabled = true;
                    _txtCDMC.Text = _configReader.GetValue("ReportTypes", "CDMCValue", "0");
                    break;
                default:
                    NotifyConsole("[WARNING]: Tipul raportului din config.ini este invalid. Setare implicită: ICT.");
                    _radioICT.Checked = true;
                    _configReader.SetValue("ReportTypes", "ReportType", "ICT");
                    _configReader.SaveConfig();
                    break;
            }

            _logFilterService.ReportType = normalizedType;
            NotifyConsole($"Tipul raportului inițial este: {normalizedType}");
            NotifyAllComponents(normalizedType);
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

        private void NotifyConsole(string message)
        {
            Console.WriteLine($"[INFO]: {message}");
            ChenarConsola.Instance?.WriteMessage(message, "INFO");
        }

        private void NotifyAllComponents(string reportType)
        {
            Form1.ChenarCaleLoguriInstance?.UpdateReportType(reportType);
            Form1.ChenarPerioadaInstance?.UpdateFieldsBasedOnReportType();
            ReportTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
