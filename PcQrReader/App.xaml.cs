using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PcQrReader
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //UI线程未捕获异常处理事件
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            //非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LoadAppOptions();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveAppoptions();
            base.OnExit(e);

            Environment.Exit(0);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
                WriteErrorLog("UI线程异常:" + e.Exception.Message);
            }
            catch (Exception ex)
            {
                //此时程序出现严重异常，将强制结束退出
                WriteErrorLog("UI线程发生致命错误！");
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StringBuilder sbEx = new StringBuilder();
            if (e.IsTerminating)
            {
                sbEx.Append("非UI线程发生致命错误");
            }
            sbEx.Append("非UI线程异常：");
            if (e.ExceptionObject is Exception)
            {
                sbEx.Append(((Exception)e.ExceptionObject).Message);
            }
            else
            {
                sbEx.Append(e.ExceptionObject);
            }
            WriteErrorLog(sbEx.ToString());
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            //task线程内未处理捕获
            WriteErrorLog("Task线程异常：" + e.Exception.Message);
            e.SetObserved();//设置该异常已察觉（这样处理后就不会引起程序崩溃）
        }

        private void WriteErrorLog(string message)
        {
            try
            {
                using (var stream = new StreamWriter("PcQrReader.error.log", true))
                {
                    stream.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] {message}");
                }
            }
            catch
            {

            }
            MessageBox.Show(message);
        }

        private void LoadAppOptions()
        {
            if (!File.Exists(AppOptions.AppSettingsPath))
            {
                Directory.CreateDirectory(AppOptions.AppDataFolder);
                return;
            }
            var settingLines = File.ReadAllLines(AppOptions.AppSettingsPath);
            var settingProps = typeof(AppOptions).GetProperties();
            foreach (var line in settingLines)
            {
                var keyValue = line.Split('=');
                if (keyValue.Length < 2)
                {
                    return;
                }
                var key = keyValue.First().Trim();
                var prop = settingProps.FirstOrDefault(p => p.CanWrite && p.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase));
                var value = Convert.ChangeType(keyValue.Last().Trim(), prop.PropertyType);
                if (prop is null)
                {
                    return;
                }
                prop.SetValue(typeof(AppOptions), value);
            }
        }

        private void SaveAppoptions()
        {
            var settingProps = typeof(AppOptions).GetProperties();
            var settingString = new StringBuilder();
            foreach (var prop in settingProps)
            {
                if (prop.CanWrite)
                {
                    settingString.AppendLine($"{prop.Name}={prop.GetValue(typeof(AppOptions))}");
                }
            }
            File.WriteAllText(AppOptions.AppSettingsPath, settingString.ToString());
        }
    }
}
