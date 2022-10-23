using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private System.Threading.Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 注意mutex不能被回收，否则就无法发挥作用了。所以只能定义在类里面。
            _mutex = new System.Threading.Mutex(true, "OnlyRun_CRNS");
            if (!_mutex.WaitOne(0, false))
            {
                MessageBox.Show("已有一个程序实例运行");
                Environment.Exit(0);
            }

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

            var tempFiles = Directory.GetFiles(AppOptions.AppDataFolder).Where(f => Path.GetFileName(f).StartsWith(AppOptions.TempFileSuffix));
            Parallel.ForEach(
                tempFiles,
                tempFile => File.Delete(tempFile));
            Environment.Exit(0);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
                WriteErrorLog("UI线程异常:" + e.Exception.Message, true);
            }
            catch (Exception ex)
            {
                //此时程序出现严重异常，将强制结束退出
                WriteErrorLog("UI线程发生致命错误！", true);
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
            WriteErrorLog(sbEx.ToString(), true);
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            //task线程内未处理捕获
            WriteErrorLog("Task线程异常：" + e.Exception.Message, true);
            e.SetObserved();//设置该异常已察觉（这样处理后就不会引起程序崩溃）
        }

        private void WriteErrorLog(string message, bool showMessage)
        {
            try
            {
                using (var stream = new StreamWriter(AppOptions.ErrorLogFile, true))
                {
                    stream.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {message}");
                }
            }
            catch
            {

            }
            if (showMessage)
            {
                MessageBox.Show(message);
            }
        }

        private void LoadAppOptions()
        {
            try
            {
                if (!File.Exists(AppOptions.AppSettingsFile))
                {
                    Directory.CreateDirectory(AppOptions.AppDataFolder);
                    return;
                }
                var settingLines = File.ReadAllLines(AppOptions.AppSettingsFile);
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
                    var stringValue = keyValue.Last().Trim();
                    var attr = prop.GetCustomAttribute<PersistenceAttribute>();
                    if (attr?.Readable == true)
                    {
                        attr.SetValue(prop, stringValue);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog(ex.ToString(), false);
                var result = MessageBox.Show(
                    "加载配置文件失败，点击确定重置配置文件，点击取消手动修复配置文件", "二维码识别器", MessageBoxButton.OKCancel, MessageBoxImage.Stop);
                if (result == MessageBoxResult.OK)
                {
                    File.Delete(AppOptions.AppSettingsFile);
                    //System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);

                    var Info = new System.Diagnostics.ProcessStartInfo();
                    Info.Arguments = "/C choice /C Y /N /D Y /T 1 & START \"\" \"" + Assembly.GetEntryAssembly().Location + "\"";
                    Info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    Info.CreateNoWindow = true;
                    Info.FileName = "cmd.exe";
                    System.Diagnostics.Process.Start(Info);
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    System.Diagnostics.Process.Start(AppOptions.AppSettingsFile);
                }
                //Application.Current.Shutdown();
                Environment.Exit(0);
            }
        }

        private void SaveAppoptions()
        {
            var settingProps = typeof(AppOptions).GetProperties();
            var settingString = new StringBuilder();
            foreach (var prop in settingProps)
            {
                var attr = prop.GetCustomAttribute<PersistenceAttribute>();
                if (attr?.Writeable == true)
                {
                    settingString.AppendLine($"{prop.Name}={attr.GetString(prop)}");
                }
            }
            File.WriteAllText(AppOptions.AppSettingsFile, settingString.ToString());
        }
    }
}
