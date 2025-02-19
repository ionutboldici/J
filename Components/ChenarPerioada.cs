using System;
using System.Globalization;
using System.Windows.Forms;
using J100.Services;

namespace J100.Components
{
    public class ChenarPerioada : UserControl
    {
        private DateTimePicker _startDatePicker;
        private DateTimePicker _endDatePicker;
        private Label _titleLabel;
        private LogFilterService _logFilterService;
        private ConfigReader _configReader;

        public ChenarPerioada(LogFilterService logFilterService, ConfigReader configReader)
        {
            _logFilterService = logFilterService;
            _configReader = configReader;
            InitializeComponents();
            LoadInitialPeriod();
        }

        private void InitializeComponents()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10, 0, 10, 0)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _titleLabel = new Label
            {
                Text = "Selectează perioada raport",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Calibri", 10, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            layout.SetColumnSpan(_titleLabel, 2);

            var startLabel = new Label
            {
                Text = "Data Start:",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Calibri", 9)
            };

            _startDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/dd/yyyy HH:mm:ss",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Calibri", 9),
                Enabled = true
            };
            _startDatePicker.ValueChanged += OnPeriodChanged;

            var endLabel = new Label
            {
                Text = "Data End:",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("Calibri", 9)
            };

            _endDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MM/dd/yyyy HH:mm:ss",
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Calibri", 9),
                Enabled = true
            };
            _endDatePicker.ValueChanged += OnPeriodChanged;

            layout.Controls.Add(_titleLabel, 0, 0);
            layout.Controls.Add(startLabel, 0, 1);
            layout.Controls.Add(_startDatePicker, 1, 1);
            layout.Controls.Add(endLabel, 0, 2);
            layout.Controls.Add(_endDatePicker, 1, 2);

            this.Controls.Add(layout);
        }

        private void LoadInitialPeriod()
        {
            var startDate = _configReader.GetValue("Period", "StartDate", DateTime.Now.ToString("MM/dd/yyyy"));
            var startTime = _configReader.GetValue("Period", "StartTime", "00:00:00");
            var endDate = _configReader.GetValue("Period", "EndDate", DateTime.Now.ToString("MM/dd/yyyy"));
            var endTime = _configReader.GetValue("Period", "EndTime", "23:59:59");

            if (DateTime.TryParseExact($"{startDate} {startTime}", "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDateTime))
            {
                _startDatePicker.Value = startDateTime;
                _logFilterService.PeriodStart = startDateTime;
            }

            if (DateTime.TryParseExact($"{endDate} {endTime}", "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateTime))
            {
                _endDatePicker.Value = endDateTime;
                _logFilterService.PeriodEnd = endDateTime;
            }

            UpdateFieldsBasedOnReportType();
        }

        private void OnPeriodChanged(object sender, EventArgs e)
        {
            if (_startDatePicker.Value > _endDatePicker.Value)
            {
                MessageBox.Show("Data Start trebuie să fie mai mică sau egală cu Data End.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _logFilterService.PeriodStart = _startDatePicker.Value;
            _logFilterService.PeriodEnd = _endDatePicker.Value;

            _configReader.SetValue("Period", "StartDate", _startDatePicker.Value.ToString("MM/dd/yyyy"));
            _configReader.SetValue("Period", "StartTime", _startDatePicker.Value.ToString("HH:mm:ss"));
            _configReader.SetValue("Period", "EndDate", _endDatePicker.Value.ToString("MM/dd/yyyy"));
            _configReader.SetValue("Period", "EndTime", _endDatePicker.Value.ToString("HH:mm:ss"));
            _configReader.SaveConfig();

            Console.WriteLine($"[INFO]: Perioada a fost actualizată: {_startDatePicker.Value} - {_endDatePicker.Value}");
        }

        public void UpdateFieldsBasedOnReportType()
        {
            var reportType = _logFilterService.ReportType;
            bool isConcatenare = reportType.Equals("Concatenare", StringComparison.OrdinalIgnoreCase);

            _startDatePicker.Enabled = !isConcatenare;
            _endDatePicker.Enabled = !isConcatenare;

            if (isConcatenare)
            {
                _logFilterService.PeriodStart = DateTime.MinValue;
                _logFilterService.PeriodEnd = DateTime.MaxValue;

                Console.WriteLine("[INFO]: Perioada a fost resetată pentru tipul de raport 'Concatenare'.");
            }
        }
    }
}
