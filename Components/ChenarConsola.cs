using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace J100.Components
{
    public class ChenarConsola : UserControl
    {
        private RichTextBox _consoleTextBox;
        private string _logFilePath;
        private int _maxMessages;

        // Singleton - instanța unică a clasei ChenarConsola
        private static ChenarConsola _instance;
        public static ChenarConsola Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Dacă instanța nu există, o creăm
                    _instance = new ChenarConsola();
                }
                return _instance;
            }
        }

        // Coada pentru păstrarea mesajelor și culorilor lor
        private readonly Queue<(string Message, System.Drawing.Color Color)> _messageQueue = new Queue<(string, System.Drawing.Color)>();

        public ChenarConsola(string logFilePath = "log_consola.txt", int maxMessages = 100)
        {
            _logFilePath = logFilePath;
            _maxMessages = maxMessages;
            InitializeComponents();
            WriteMessage("Clasa ChenarConsola a fost inițializată corect.", "INFO");
        }

        private void InitializeComponents()
        {
            // Configurarea RichTextBox-ului pentru consolă
            _consoleTextBox = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White // Default color
            };

            this.Controls.Add(_consoleTextBox);
        }

        public void WriteMessage(string message, string type = "INFO")
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formattedMessage = $"[{timestamp}] [{type}] {message}";
            System.Drawing.Color messageColor = GetMessageColor(type);

            // Salvăm mesajul și culoarea în coadă dacă nu este duplicat
            if (!_messageQueue.Any(m => m.Message == formattedMessage))
            {
                _messageQueue.Enqueue((formattedMessage, messageColor));
                AppendColoredText(formattedMessage, messageColor);
                EnforceMessageLimit();
            }

            // Derulare automată doar pentru ultima linie
            ScrollToLastMessage();
            SaveToLogFile(formattedMessage);
        }

        private System.Drawing.Color GetMessageColor(string type)
        {
            type = type.ToUpper();

            if (type == "INFO")
                return System.Drawing.Color.LightGreen;
            if (type == "WARNING")
                return System.Drawing.Color.Yellow;
            if (type == "ERROR")
                return System.Drawing.Color.Red;
            if (type == "DEBUG")
                return System.Drawing.Color.Cyan;

            return System.Drawing.Color.White;
        }

        private void AppendColoredText(string message, System.Drawing.Color color)
        {
            // Verificăm că SelectionStart nu este negativ
            if (_consoleTextBox.SelectionStart < 0)
            {
                _consoleTextBox.SelectionStart = 0; // setăm un index valid
            }

            _consoleTextBox.SelectionStart = _consoleTextBox.TextLength;
            _consoleTextBox.SelectionLength = 0;

            _consoleTextBox.SelectionColor = color;
            _consoleTextBox.AppendText(message + "\n");
        }

        private void EnforceMessageLimit()
        {
            while (_messageQueue.Count > _maxMessages)
            {
                _messageQueue.Dequeue();
            }
        }

        private void ScrollToLastMessage()
        {
            _consoleTextBox.SelectionStart = _consoleTextBox.TextLength;
            _consoleTextBox.ScrollToCaret();
        }

        private void SaveToLogFile(string message)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                WriteMessage($"Eroare la salvarea logului: {ex.Message}", "ERROR");
            }
        }

        public void ClearConsole()
        {
            _consoleTextBox.Clear();
            _messageQueue.Clear();
        }

        public void WriteError(string message)
        {
            WriteMessage(message, "ERROR");
        }

        public void WriteWarning(string message)
        {
            WriteMessage(message, "WARNING");
        }

        public void WriteSuccess(string message)
        {
            WriteMessage(message, "SUCCESS");
        }

        public void WriteDebug(string message)
        {
            WriteMessage(message, "DEBUG");
        }

        // Adăugare metodă pentru afișarea mesajelor legate de NTotal
        public void WriteNTotalUpdate(int nTotal)
        {
            WriteMessage($"Valoarea NTotal a fost actualizată: {nTotal} fișiere compatibile.", "INFO");
        }

        // Adăugare metodă pentru mesajele de activare/dezactivare filtre
        public void WriteFilterActivationMessage(string filterName, bool isActive)
        {
            string message = isActive ? $"{filterName} - Activat" : $"{filterName} - Dezactivat";
            WriteMessage(message, "INFO");
        }
    }
}
