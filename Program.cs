using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace CanXeApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Cấu hình Culture cho toàn bộ ứng dụng (Threads & UI) sang tiếng Việt (vi-VN)
            CultureInfo viCulture = new CultureInfo("vi-VN");
            viCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            viCulture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            
            Thread.CurrentThread.CurrentCulture = viCulture;
            Thread.CurrentThread.CurrentUICulture = viCulture;
            CultureInfo.DefaultThreadCurrentCulture = viCulture;
            CultureInfo.DefaultThreadCurrentUICulture = viCulture;

            ApplicationConfiguration.Initialize();
            Application.Run(new FormMain());
        }
    }
}