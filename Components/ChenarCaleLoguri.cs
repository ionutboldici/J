using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using J100.Services;

namespace J100.Components
{
    public class ChenarCaleLoguri : UserControl
    {
        private Label _labelTitle;
        private TextBox _textBoxCale;
        private Button _buttonBrowse;
        private Label _labelInfo;
        private string _logPath;
        private ChenarConsola _consola;
        private string _compatibleFiles;
        private ConfigReader _configReader;
        private LogFilterService _logFilterService; // Referință pentru LogFilterService

        public int NTotal { get; private set; }

        public event EventHandler NTotalChanged;
        public event EventHandler LogPathChanged;

        private string _reportType;
        private DateTime? _periodStart;
        private DateTime? _periodEnd;

        public ChenarCaleLoguri(ChenarConsola consola, ConfigReader configReader, string compatibleFiles, LogFilterService logFilterService)
        {
            _consola = consola ?? throw new ArgumentNullException(nameof(consola));
            _compatibleFiles = compatibleFiles ?? throw new ArgumentNullException(nameof(compatibleFiles));
            _configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            _logFilterService = logFilterService ?? throw new ArgumentNullException(nameof(logFilterService));

            _logPath = _configReader.GetValue("Paths", "LogPath", "C:\\Loguri");
            _reportType = _configReader.GetValue("ReportTypes", "ReportType", "ICT");
            InitializeComponents();
            InitializePath();
        }

        private void InitializeComponents()
        {
            this.Dock = DockStyle.Fill;

            _labelTitle = new Label
            {
                Text = "Cale loguri de analizat",
                Dock = DockStyle.Top,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 25,
                Margin = new Padding(10, 0, 0, 10)
            };

            _textBoxCale = new TextBox
            {
                ReadOnly = true,
                Width = 470,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Margin = new Padding(10, 0, 10, 0)
            };

            _buttonBrowse = new Button
            {
                Text = "Browse",
                Height = _textBoxCale.Height,
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 0)
            };

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(10, 5, 10, 10)
            };

            _textBoxCale.Location = new Point(10, 0);
            _textBoxCale.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            _buttonBrowse.Location = new Point(_textBoxCale.Right + 5, 0);
            _buttonBrowse.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            panelTop.Controls.Add(_textBoxCale);
            panelTop.Controls.Add(_buttonBrowse);

            _labelInfo = new Label
            {
                Text = $"Fișiere acceptate: {_compatibleFiles}",
                Dock = DockStyle.Bottom,
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(10, 10, 10, 0)
            };

            _buttonBrowse.Click += OnBrowseClick;

            this.Controls.Add(panelTop);
            this.Controls.Add(_labelInfo);
            this.Controls.Add(_labelTitle);
        }

        private void InitializePath()
        {
            try
            {
                ValidateAndUpdateNTotal();
            }
            catch (Exception ex)
            {
                _consola.WriteMessage($"[ERROR]: Eroare la inițializarea ChenarCaleLoguri: {ex.Message}");
            }
        }

        private void ValidateAndUpdateNTotal()
        {
            if (Directory.Exists(_logPath))
            {
                NTotal = CountCompatibleFiles(_logPath);
                OnNTotalChanged();
                _textBoxCale.Text = _logPath;
                _consola.WriteMessage($"[INFO]: Cale loguri inițializată: {_logPath}. Fișiere compatibile: {NTotal}.");
            }
            else
            {
                NTotal = 0;
                OnNTotalChanged();
                _consola.WriteMessage($"[WARNING]: Calea specificată nu este validă: {_logPath}.");
            }
        }

        private int CountCompatibleFiles(string path)
        {
            try
            {
                var compatibleExtensions = _compatibleFiles.Split(',').Select(ext => ext.Trim().ToLower()).ToArray();
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(file => compatibleExtensions.Contains(Path.GetExtension(file).ToLower()));

                if (_reportType != "Concatenare" && _periodStart.HasValue && _periodEnd.HasValue)
                {
                    files = files.Where(file =>
                    {
                        var creationDate = File.GetCreationTime(file);
                        return creationDate >= _periodStart.Value && creationDate <= _periodEnd.Value;
                    });
                }

                return files.Count();
            }
            catch (Exception ex)
            {
                _consola.WriteMessage($"[ERROR]: Eroare la numărarea fișierelor compatibile: {ex.Message}");
                return 0;
            }
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Selectați un folder care conține fișiere log compatibile";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _logPath = folderDialog.SelectedPath;
                    _textBoxCale.Text = _logPath;

                    try
                    {
                        // Actualizare în ConfigReader
                        _configReader.SetValue("Paths", "LogPath", _logPath);
                        _configReader.SaveConfig();

                        // Actualizare în LogFilterService
                        _logFilterService.UpdateLogPath(_logPath);

                        _consola.WriteMessage($"[INFO]: LogPath actualizat în config.ini și sincronizat cu LogFilterService: {_logPath}.");
                        ValidateAndUpdateNTotal();
                        OnLogPathChanged();
                    }
                    catch (Exception ex)
                    {
                        _consola.WriteMessage($"[ERROR]: Eroare la salvarea LogPath: {ex.Message}");
                    }
                }
            }
        }

        private void OnNTotalChanged()
        {
            NTotalChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnLogPathChanged()
        {
            LogPathChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateReportType(string reportType, DateTime? periodStart = null, DateTime? periodEnd = null)
        {
            _reportType = reportType;
            _periodStart = periodStart;
            _periodEnd = periodEnd;

            ValidateAndUpdateNTotal();
        }
    }
}
