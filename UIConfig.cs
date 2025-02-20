using System.Drawing;
using System.Windows.Forms;

public static class UIConfig
{
    // Fonturi
    public static readonly Font DefaultFont = new Font("Microsoft Sans Serif", 8); // Font standard
    public static readonly Font TitleFont = new Font("Microsoft Sans Serif", 9, FontStyle.Bold); // Font pentru titluri

    // Dimensiuni implicite
    public const int RowHeight = 25; // Înălțimea rândurilor
    public const int ButtonHeight = 25; // Înălțimea butoanelor standard
    public const int TextBoxHeight = 24; // Înălțimea textbox-urilor
    public const int LabelHeight = 20; // Înălțimea label-urilor
    public const int CheckboxHeight = 20; // Înălțimea checkbox-urilor

    // Dimensiuni pentru butonul Start
    public const int StartButtonHeight = 40; // Înălțime personalizată pentru butonul Start
    public const int StartButtonWidth = 150; // Lățime personalizată pentru butonul Start

    // Dimensiuni pentru TextBox-ul CDMC
    public const int TextBoxCDMCWidth = 300;  // Lățimea textbox-ului CDMC (ex: 300px)
    public const int TextBoxCDMCHeight = 25;  // Înălțimea textbox-ului CDMC (ex: 25px)

    // Lățimi implicite (opțional, dacă este nevoie)
    public const int ButtonWidth = 75;
    public const int TextBoxWidth = 150;

    // Borduri pentru chenare
    public const BorderStyle DefaultBorderStyle = BorderStyle.Fixed3D;  // Bordură 3D implicită pentru chenare

    // Spațiere și margin/padding
    public static readonly Padding DefaultPadding = new Padding(0, 5, 0, 5); // Spațiere între controale
    public static readonly Padding CheckboxLabelMargin = new Padding(5, 0, 0, 0); // Distanță între checkbox și label

    // Aliniere
    public static readonly ContentAlignment TextAlignLeft = ContentAlignment.MiddleLeft; // Aliniere la stânga
    public static readonly ContentAlignment TextAlignCenter = ContentAlignment.MiddleCenter; // Aliniere centrată

    // Culori implicite
    public static readonly Color DefaultBackgroundColor = Color.White; // Fundal alb pentru toate controalele
    public static readonly Color DefaultTextColor = Color.Black; // Culoare text implicită
    public static readonly Color ErrorTextColor = Color.Red; // Culoare pentru erori
    public static readonly Color InfoTextColor = Color.Green; // Culoare pentru mesaje informative

    // Alte proprietăți
    public const ScrollBars DefaultScrollBars = ScrollBars.Vertical; // Scroll vertical implicit

}
