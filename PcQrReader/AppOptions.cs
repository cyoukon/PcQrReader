using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PcQrReader
{
    public static class AppOptions
    {
        /// <summary>
        /// 公司名称
        /// </summary>
        [NonSerialized]
        public static readonly string CompanyName = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false))
                .Company;

        /// <summary>
        /// 应用数据目录
        /// </summary>
        [NonSerialized]
        public static readonly string AppDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CompanyName,
                Assembly.GetExecutingAssembly().GetName().Name);

        /// <summary>
        /// 应用设置文件
        /// </summary>
        [NonSerialized]
        public static readonly string AppSettingsPath = Path.Combine(AppDataFolder, "appSettings.txt");

        /// <summary>
        /// 左网格占比
        /// </summary>
        public static double LeftGridRatio { get; set; } = 1;

        /// <summary>
        /// 右网格占比
        /// </summary>
        public static double RightGridRatio { get; set; } = 1;

        /// <summary>
        /// 上网格占比
        /// </summary>
        public static double UpGridRatio { get; set; } = 2;

        /// <summary>
        /// 下网格占比
        /// </summary>
        public static double DownGridRatio { get; set; } = 1;

        /// <summary>
        /// 窗体宽度
        /// </summary>
        public static double Width { get; set; } = 600;

        /// <summary>
        /// 窗体高度
        /// </summary>
        public static double Height { get; set; } = 350;
    }
}
