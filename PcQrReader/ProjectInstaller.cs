using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace PcQrReader
{
    /// <summary>
    /// 安装包处理类
    /// </summary>
    /// <see cref="https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2022InstallerProjects"/>
    /// <seealso cref="https://www.cnblogs.com/1175429393wljblog/p/13229438.html"/>
    /// <seealso cref="https://www.c-sharpcorner.com/article/how-to-perform-custom-actions-and-upgrade-using-visual-studio-installer/"/>
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            //ServiceInstaller serviceInstaller = new ServiceInstaller();
            //InstallContext installContext = new InstallContext(AppOptions.AppDataFolder, null);
            //serviceInstaller.Context = Context;
            //serviceInstaller.Uninstall(null);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            try
            {
                Directory.CreateDirectory(AppOptions.AppDataFolder);
            }
            catch
            {

            }
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            try
            {
                Directory.Delete(AppOptions.AppDataFolder, true);
            }
            catch
            {

            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message"></param>
        private static void Logger(string message)
        {
            try
            {
                string fileName = @"D:\temp\log.txt";
                if (!File.Exists(fileName))
                {
                    File.Create(fileName);
                    Trace.Listeners.Clear();
                    Trace.AutoFlush = true;
                    Trace.Listeners.Add(new TextWriterTraceListener(fileName));
                }
                Trace.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + message);
            }
            catch (Exception ex)
            {
                Trace.Listeners.Clear();
                Trace.AutoFlush = true;
                Trace.Listeners.Add(new TextWriterTraceListener(@"D:\temp\log.txt"));
                Trace.WriteLine($"Logger出错，错误信息：{ex}");
            }
        }
    }
}
