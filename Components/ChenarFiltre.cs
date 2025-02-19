using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using J100.Services;

namespace J100.Components
{
    public class ChenarFiltre : UserControl
    {
        private Dictionary<int, TextBox> _filterTextBoxes; // Index pe baza liniei (1, 2, etc.)
        private Dictionary<int, Button> _saveButtons; // Index pe baza liniei
        private Dictionary<int, CheckBox> _filterCheckBoxes; // CheckBox pentru activare/inactivare
        private Label _titleLabel;
        private LogFilterService _logFilterService;
        private ConfigReader _configReader;
        private ChenarConsola _chenarConsola;

        // Dicționar pentru descrierile fiecărui filtru
        private readonly Dictionary<int, string> _filterDescriptions = new Dictionary<int, string>
        {
            { 1, "IF Start OR End with" },
            { 2, "OR Contain" },
            { 3, "AND Contain" },
            { 4, "AND NOT Contain" }
        };

        public event EventHandler FiltersChanged;

        public ChenarFiltre(LogFilterService logFilterService, ConfigReader configReader)
        {
            _logFilterService = logFilterService;
            _configReader = configReader;
            _chenarConsola = ChenarConsola.Instance;
            InitializeComponents();
            LoadInitialFilters();

            // Abonare la schimbarea tipului de raport
            _logFilterService.ReportTypeChanged += OnReportTypeChanged;
        }

        private void InitializeComponents()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = _filterDescriptions.Count + 2,
                Padding = UIConfig.DefaultPadding
            };

            // Configurare proporții coloane
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 4)); // CheckBox
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26)); // Label
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // TextBox
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10)); // Buton

            // Configurare rânduri cu înălțime fixă din UIConfig
            for (int i = 0; i < layout.RowCount; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, UIConfig.RowHeight));
            }

            // Titlu
            _titleLabel = new Label
            {
                Text = "Reguli de filtrare și selectare:",
                Dock = DockStyle.Fill,
                TextAlign = UIConfig.TextAlignLeft,
                Font = UIConfig.TitleFont,
                AutoSize = false,
                Height = UIConfig.LabelHeight
            };
            layout.Controls.Add(_titleLabel, 0, 0);
            layout.SetColumnSpan(_titleLabel, 4);

            _filterTextBoxes = new Dictionary<int, TextBox>();
            _saveButtons = new Dictionary<int, Button>();
            _filterCheckBoxes = new Dictionary<int, CheckBox>();

            foreach (var filter in _filterDescriptions)
            {
                // Checkbox
                var checkBox = new CheckBox
                {
                    Dock = DockStyle.Fill,
                    Checked = true // Activat implicit
                };
                checkBox.CheckedChanged += (sender, e) => OnFilterCheckBoxChanged(filter.Key);

                // Label
                var label = new Label
                {
                    Text = $"Filter{filter.Key}: {filter.Value}",
                    Dock = DockStyle.Fill,
                    TextAlign = UIConfig.TextAlignLeft,
                    Font = UIConfig.DefaultFont,
                    Margin = UIConfig.CheckboxLabelMargin
                };

                // TextBox
                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = UIConfig.DefaultFont,
                    Height = UIConfig.TextBoxHeight,
                    Enabled = true // Implicit activ
                };
                textBox.TextChanged += (sender, e) => OnFilterTextBoxChanged();

                // Buton
                var saveButton = new Button
                {
                    Text = "Salvare",
                    Font = UIConfig.DefaultFont,
                    Height = UIConfig.ButtonHeight,
                    Width = UIConfig.ButtonWidth, // Asigurare dimensiune corectă
                    TextAlign = UIConfig.TextAlignLeft,
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat // Stil consistent
                };

                int currentIndex = filter.Key;
                saveButton.Click += (sender, e) => SaveFilterToConfig(currentIndex, textBox.Text);

                _filterCheckBoxes[currentIndex] = checkBox;
                _filterTextBoxes[currentIndex] = textBox;
                _saveButtons[currentIndex] = saveButton;

                layout.Controls.Add(checkBox, 0, currentIndex);
                layout.Controls.Add(label, 1, currentIndex);
                layout.Controls.Add(textBox, 2, currentIndex);
                layout.Controls.Add(saveButton, 3, currentIndex);
            }

            this.Controls.Add(layout);
        }

        public void LoadInitialFilters()
        {
            var reportType = GetFullReportType();

            try
            {
                foreach (var filter in _filterDescriptions.Keys)
                {
                    var filterKey = $"Filter{filter}{reportType}";
                    var configValue = _configReader.GetValue("Filters", filterKey, string.Empty);

                    if (!string.IsNullOrWhiteSpace(configValue))
                    {
                        _filterTextBoxes[filter].Text = configValue;
                        _filterCheckBoxes[filter].Checked = true;
                    }
                    else
                    {
                        _filterTextBoxes[filter].Text = string.Empty;
                        _filterCheckBoxes[filter].Checked = false;
                    }
                }
                FiltersChanged?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("[DEBUG]: LoadInitialFilters a fost apelată.");
            }
            catch (Exception ex)
            {
                LogError($"[ERROR]: Eroare la încărcarea filtrelor: {ex.Message}");
            }
        }

        private void SaveFilterToConfig(int filterIndex, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine($"[WARNING]: Valoarea pentru Filter{filterIndex} este goală. Nu se salvează nimic.");
                return;
            }

            var reportType = GetFullReportType();
            var filterKey = $"Filter{filterIndex}{reportType}";

            try
            {
                _configReader.SetValue("Filters", filterKey, value);
                _configReader.SaveConfig();

                Console.WriteLine($"[INFO]: Filtrul {filterKey} a fost salvat cu valoarea: {value}");
                FiltersChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: Eroare la salvarea filtrului {filterKey}: {ex.Message}");
            }
        }

        private void OnFilterCheckBoxChanged(int filterIndex)
        {
            if (!_filterCheckBoxes[filterIndex].Checked)
            {
                _logFilterService.Filters[$"Filter{filterIndex}"] = null; // Valoare null pentru aplicație
                _filterTextBoxes[filterIndex].Enabled = false; // Dezactivează TextBox
                _saveButtons[filterIndex].Enabled = false; // Dezactivează butonul de salvare
                _chenarConsola.WriteMessage($"[INFO]: Filtru{filterIndex} - Dezactivat");
            }
            else
            {
                _filterTextBoxes[filterIndex].Enabled = true;
                _saveButtons[filterIndex].Enabled = true;
                _logFilterService.Filters[$"Filter{filterIndex}"] = _filterTextBoxes[filterIndex].Text;
                _chenarConsola.WriteMessage($"[INFO]: Filtru{filterIndex} - Activat");
            }

            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnFilterTextBoxChanged()
        {
            FiltersChanged?.Invoke(this, EventArgs.Empty);
            Console.WriteLine("[DEBUG]: OnFilterTextBoxChanged a fost apelat.");
        }

        private void OnReportTypeChanged(object sender, EventArgs e)
        {
            Console.WriteLine("[INFO]: Tipul de raport a fost modificat. Se reîncarcă filtrele.");
            LoadInitialFilters();
        }

        private string GetFullReportType()
        {
            var shortReportType = _logFilterService.ReportType.ToUpper();

            if (shortReportType == "I" || shortReportType == "ICT")
                return "ICT";
            if (shortReportType == "E" || shortReportType == "EOL")
                return "EOL";
            if (shortReportType == "C" || shortReportType == "CUSTOM")
                return "Custom";
            if (shortReportType == "CON" || shortReportType == "CONCATENARE")
                return "Concatenare";

            throw new ArgumentException("Tip de raport necunoscut.");
        }

        public bool AreFiltersValid()
        {
            foreach (var textBox in _filterTextBoxes.Values)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                    return false;
            }
            return true;
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
