// ---
// Summary:
// - Purpose: Application entry point.
// - Role: Startup coordinator.
// - Used by: Windows OS process host.
// - Depends on: System.Windows.Forms, KeyboardControl.UI.MainForm.
// - Key Responsibilities: Enable visual styles and launch MainForm.
// - Notes: Targets .NET Framework 4.8.
// ---

using System;
using System.Windows.Forms;
using KeyboardControl.UI;

namespace KeyboardControl
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}