using System;
using System.Threading;
using System.Windows.Forms;

namespace RunCat {
    internal static class Program {
        private static Mutex processMutex;

        [STAThread]
        internal static void Main() {
            // Pre-configure visual 
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Acquire application mutex to make only one instance of the app running at a time.
            processMutex = new Mutex(true, Constants.MUTEX, out bool processMutexResult);
            if(!processMutexResult) {
                // If the mutex can't be acquired, show a message box and terminate current instance of the app.
                MessageBox.Show(
                    string.Format(Strings.Strings.Message_AlreadyRunning_Description, Application.ProductName),
                    string.Format(Strings.Strings.Message_AlreadyRunning_Title, Application.ProductName),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Set up application event(s)
            Application.ApplicationExit += OnApplicationExit;

            // Start application context if there's all good
            Application.Run(new RunCatApplicationContext());
        }

        private static void OnApplicationExit(object sender, EventArgs e) {
            // Release mutex on application exit
            if(processMutex != null) {
                processMutex.ReleaseMutex();
            }
        }
    }

    internal class RunCatApplicationContext : ApplicationContext {
        public RunCatApplicationContext() {
            // TODO
        }
    }
}
