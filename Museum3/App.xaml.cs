using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance;
            _mutex = new Mutex(true, "MuseumInteractiveApp_Mutex", out isNewInstance);

            if (!isNewInstance)
            {
                // Приложение уже запущено — активируем его
                BringExistingInstanceToFront();
                Shutdown(); // Выходим из нового экземпляра
                return;
            }

            base.OnStartup(e);
        }

        private void BringExistingInstanceToFront()
        {
            // Заменить "MainWindow" на заголовок окна или имя класса, если нужно точнее
            IntPtr hWnd = FindWindow(null, "MainWindow");

            if (hWnd != IntPtr.Zero)
            {
                if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }
        }
    }
}
