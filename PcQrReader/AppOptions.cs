using System;
using System.Collections;
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
        public static readonly string AppSettingsFile = Path.Combine(AppDataFolder, "appSettings.txt");

        /// <summary>
        /// 错误日志文件
        /// </summary>
        [NonSerialized]
        public static readonly string ErrorLogFile = Path.Combine(AppDataFolder, "PcQrReader.error.log");

        /// <summary>
        /// 临时文件前缀
        /// </summary>
        [NonSerialized]
        public static readonly string TempFileSuffix = ".Temp_";

        /// <summary>
        /// 左网格占比
        /// </summary>
        [Persistence(true, true)]
        public static double LeftGridRatio { get; set; } = 1;

        /// <summary>
        /// 右网格占比
        /// </summary>
        [Persistence(true, true)]
        public static double RightGridRatio { get; set; } = 1;

        /// <summary>
        /// 上网格占比
        /// </summary>
        [Persistence(true, true)]
        public static double UpGridRatio { get; set; } = 2;

        /// <summary>
        /// 下网格占比
        /// </summary>
        [Persistence(true, true)]
        public static double DownGridRatio { get; set; } = 1;

        /// <summary>
        /// 窗体宽度
        /// </summary>
        [Persistence(true, true)]
        public static double Width { get; set; } = 600;

        /// <summary>
        /// 窗体高度
        /// </summary>
        [Persistence(true, true)]
        public static double Height { get; set; } = 350;

        /// <summary>
        /// 窗体左边缘相对于桌面的位置
        /// </summary>
        [Persistence(true, true)]
        public static double Left { get; set; }

        /// <summary>
        /// 窗体上边缘相对于桌面的位置
        /// </summary>
        [Persistence(true, true)]
        public static double Top { get; set; }

        /// <summary>
        /// 图片类型文件后缀
        /// </summary>
        [PersistenceValueList(true, true)]
        public static List<string> PictureSuffix { get; set; } = new List<string> { "*.jpg", "*.png", "*.tif", "*.tiff", "*.gif" };
    }

    /// <summary>
    /// 用于标识属性可以持久化读写的属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PersistenceAttribute : Attribute
    {
        /// <summary>
        /// 可持久化读
        /// </summary>
        public virtual bool Readable { get; }
        /// <summary>
        /// 可持久化写
        /// </summary>
        public virtual bool Writeable { get; }

        public PersistenceAttribute(bool readable, bool writeable)
        {
            Readable = readable;
            Writeable = writeable;
        }

        public virtual string GetString(PropertyInfo prop) => prop.GetValue(typeof(AppOptions)).ToString();

        public virtual void SetValue(PropertyInfo prop, string stringValue)
        {
            if (prop is null)
            {
                return;
            }
            var value = Convert.ChangeType(stringValue, prop.PropertyType);
            prop.SetValue(typeof(AppOptions), value);
        }
    }

    public class PersistenceValueListAttribute : PersistenceAttribute
    {
        private readonly string _separator = ",";

        public PersistenceValueListAttribute(bool readable, bool writeable) : base(readable, writeable)
        {
        }

        public override string GetString(PropertyInfo prop)
        {
            if (prop is null)
            {
                return string.Empty;
            }
            try
            {
                var value = prop.GetValue(typeof(AppOptions)) as IList;
                var type = prop.PropertyType.GetGenericArguments().First();
                if (prop.PropertyType.IsGenericType
                    && (type == typeof(string) || type?.IsValueType == true))
                {
                    var ret = string.Join(_separator, value.Cast<string>());
                    return ret;
                }
            }
            catch
            {
            }
            return base.GetString(prop);
        }

        public override void SetValue(PropertyInfo prop, string stringValue)
        {
            if (prop is null)
            {
                return;
            }
            try
            {
                if (Activator.CreateInstance(prop.PropertyType) is System.Collections.IList value)
                {
                    var type = prop.PropertyType.GetGenericArguments().First();
                    foreach (var item in stringValue.Split(new[] { _separator }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var listItem = Convert.ChangeType(item, type);
                        value.Add(listItem);
                    }
                    prop.SetValue(typeof(AppOptions), value);
                    return;
                }
            }
            catch
            {
            }
            base.SetValue(prop, stringValue);
        }
    }
}
