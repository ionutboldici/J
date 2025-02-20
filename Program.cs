using System;
using System.Windows.Forms;

namespace J100
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.Run(new Form1());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("A apărut o eroare neașteptată: " + e.Exception.Message, "Eroare Aplicație", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
