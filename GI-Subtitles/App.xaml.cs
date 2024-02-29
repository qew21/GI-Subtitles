using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace GI_Subtitles
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "GI-Subtitles";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // 程序已经在运行，显示提示信息
                var result = MessageBox.Show("程序已经在运行，是否关闭正在运行的程序？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // 关闭正在运行的程序实例
                    Process current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName).Where(p => p.Id != current.Id))
                    {
                        process.Kill();
                    }
                }
                else
                {
                    // 直接退出当前程序实例
                    Shutdown();
                    return;
                }
            }

            base.OnStartup(e);
        }
    }
}
