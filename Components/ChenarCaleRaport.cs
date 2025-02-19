using System;
using System.IO;
using System.Windows.Forms;
using J100.Services;

namespace J100.Components
{
    public class ChenarCaleRaport : UserControl
    {
        private Label _titleLabel;
        private TextBox _textBoxPath;
        private Button _buttonBrowse;
        private CheckBox _checkBoxTxt;
        private CheckBox _checkBoxXlsx;
        private Label _labelInfo;

        private ConfigReader _configReader;
        private ChenarConsola _chenarConsola;
        private LogFilterService _logFilterService; // Referință pentru LogFilterService
        private string _defaultPath;

        // Eveniment pentru notificarea schimbării ReportPath
        public event EventHandler ReportPathChanged;

        public ChenarCaleRaport(ConfigReader configReader, ChenarConsola chenarConsola, LogFilterService logFilterService)
        {
            if (configReader == null)
                throw new ArgumentNullException(nameof(configReader), "ConfigReader nu poate fi null.");
            if (chenarConsola == null)
                throw new ArgumentNullException(nameof(chenarConsola), "ChenarConsola nu poate fi null.");
            if (logFilterService == null)
                throw new ArgumentNullException(nameof(logFilterService), "LogFilterService nu poate fi null.");

            _configReader = configReader;
            _chenarConsola = chenarConsola;
            _logFilterService = logFilterService;
            _defaultPath = configReader.GetValue("Paths", "ReportPath", Environment.CurrentDirectory);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _titleLabel = new Label
            {
                Text = "Cale Ieșire Raport",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Calibri", 12, System.Drawing.FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5),
                Height = 30
            };

            var panelPath = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10, 5, 10, 5)
            };

            _textBoxPath = new TextBox
            {
                Text = _defaultPath,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Calibri", 9),
                BackColor = System.Drawing.Color.White,
                Height = 30
            };

            _buttonBrowse = new Button
            {
                Text = "Browse",
                Dock = DockStyle.Right,
                Font = new System.Drawing.Font("Calibri", 9),
                BackColor = System.Drawing.Color.LightGray,
                Width = 75,
                Height = 30
            };
            _buttonBrowse.Click += OnBrowseClick;

            panelPath.Controls.Add(_textBoxPath);
            panelPath.Controls.Add(_buttonBrowse);

            _checkBoxTxt = new CheckBox
            {
                Text = ".txt",
                Checked = _configReader.GetValue("OutputFormats", "Txt", "true") == "true",
                Dock = DockStyle.Left,
                Font = new System.Drawing.Font("Calibri", 9),
                Padding = new Padding(10, 5, 10, 5)
            };
            _checkBoxTxt.CheckedChanged += OnFormatChanged;

            _checkBoxXlsx = new CheckBox
            {
                Text = ".xlsx",
                Checked = _configReader.GetValue("OutputFormats", "Xlsx", "true") == "true",
                Dock = DockStyle.Left,
                Font = new System.Drawing.Font("Calibri", 9),
                Padding = new Padding(10, 5, 10, 5)
            };
            _checkBoxXlsx.CheckedChanged += OnFormatChanged;

            _labelInfo = new Label
            {
                Text = "Formate disponibile: [.txt, .xlsx]",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Calibri", 9),
                ForeColor = System.Drawing.Color.DarkGray,
                Padding = new Padding(10, 5, 10, 5)
            };

            Controls.Add(_labelInfo);
            Controls.Add(_checkBoxXlsx);
            Controls.Add(_checkBoxTxt);
            Controls.Add(panelPath);
            Controls.Add(_titleLabel);
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var selectedPath = folderDialog.SelectedPath;
                    if (Directory.Exists(selectedPath))
                    {
                        _textBoxPath.Text = selectedPath;

                        // Actualizare în ConfigReader
                        _configReader.SetValue("Paths", "ReportPath", selectedPath);
                        _configReader.SaveConfig();

                        // Notificare către LogFilterService
                        _logFilterService.UpdateReportPath(selectedPath);

                        // Emitere eveniment ReportPathChanged
                        ReportPathChanged?.Invoke(this, EventArgs.Empty);

                        _chenarConsola.WriteMessage($"[INFO]: Folderul de ieșire selectat: {selectedPath}");
                    }
                    else
                    {
                        _chenarConsola.WriteError("[ERROR]: Folderul selectat este inaccesibil.");
                    }
                }
            }
        }

        private void OnFormatChanged(object sender, EventArgs e)
        {
            var txtChecked = _checkBoxTxt.Checked;
            var xlsxChecked = _checkBoxXlsx.Checked;

            _configReader.SetValue("OutputFormats", "Txt", txtChecked.ToString().ToLower());
            _configReader.SetValue("OutputFormats", "Xlsx", xlsxChecked.ToString().ToLower());
            _configReader.SaveConfig();

            _chenarConsola.WriteMessage($"[INFO]: Formate selectate - TXT: {txtChecked}, XLSX: {xlsxChecked}");
        }

        public void SaveFormats()
        {
            var txtChecked = _checkBoxTxt.Checked;
            var xlsxChecked = _checkBoxXlsx.Checked;

            if (!txtChecked && !xlsxChecked)
            {
                _chenarConsola.WriteError("[ERROR]: Niciun format de ieșire selectat.");
                return;
            }

            _configReader.SetValue("OutputFormats", "Txt", txtChecked.ToString().ToLower());
            _configReader.SetValue("OutputFormats", "Xlsx", xlsxChecked.ToString().ToLower());
            _configReader.SaveConfig();
            _chenarConsola.WriteMessage("[INFO]: Formatele de ieșire au fost salvate cu succes.");
        }
    }
}
