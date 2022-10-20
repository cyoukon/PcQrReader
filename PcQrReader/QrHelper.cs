using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace PcQrReader
{
    internal class QrHelper : IDisposable
    {
        public static Bitmap img;
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoCaptureDevice_on;

        /// <summary>
        /// 获取设备mac地址
        /// </summary>
        /// <returns></returns>
        private string GetMac()
        {
            try
            {
                string strMac = string.Empty;
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        strMac = mo["MacAddress"].ToString();
                    }
                }
                moc = null;
                mc = null;
                return strMac;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 获取可使用摄像列表
        /// </summary>
        /// <returns></returns>
        public List<CustomFilterInfo> GetFilterInfo()
        {
            List<CustomFilterInfo> filterInfos;
            try
            {
                //AForge.Video.DirectShow.FilterInfoCollection 设备枚举类
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_videoDevices.Count == 0)
                    return null;
                filterInfos = new List<CustomFilterInfo>();
                for (int i = 0; i < _videoDevices.Count; i++)
                {
                    filterInfos.Add(CustomFilterInfo.FromFilterInfo(_videoDevices[i]));
                }
            }
            catch (ApplicationException ex)
            {
                throw ex;
            }
            return filterInfos;
        }

        /// <summary>
        /// 打开任一设备（此时即可委托得到打开设备后的图片）
        /// </summary>
        /// <param name="device"></param>
        public VideoCaptureDevice OpenDevice(FilterInfo device)
        {
            CloseVideoSource(ref _videoCaptureDevice_on);
            _videoCaptureDevice_on = new VideoCaptureDevice(device.MonikerString);
            _videoCaptureDevice_on.NewFrame += new NewFrameEventHandler(VideoNewFrame);
            CloseVideoSource(ref _videoCaptureDevice_on);
            _videoCaptureDevice_on.Start();
            return _videoCaptureDevice_on;
        }

        /// <summary>
        /// 识别二维码
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public string ScanBarcode(Bitmap bitmap)
        {
            if (bitmap is null)
            {
                return string.Empty;
            }

            //设置读取二维码
            //DecodingOptions decodeOption = new DecodingOptions();
            //decodeOption.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };

            //读取操作
            BarcodeReader reader = new BarcodeReader();
            //reader.Options = decodeOption;
            ZXing.Result result = reader.Decode(bitmap);

            return result?.Text;
        }

        /// <summary>
        /// 识别二维码
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string ScanBarcode(string path)
        {
            Bitmap bitmap = new Bitmap(@path);
            return ScanBarcode(bitmap);
        }

        private void VideoNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            img = (Bitmap)eventArgs.Frame.Clone();
        }

        private void CloseVideoSource(ref VideoCaptureDevice videoSource)
        {
            if (!(videoSource == null))
            {
                if (videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    videoSource = null;
                }
            }
        }

        public void CloseVideoSource()
        {
            CloseVideoSource(ref _videoCaptureDevice_on);
        }

        public void Dispose()
        {
            CloseVideoSource(ref _videoCaptureDevice_on);
        }

        /// <summary>
        /// 生成二维码,保存成图片
        /// </summary>
        public Bitmap GenerateQr(string text)
        {
            BarcodeWriter writer = new BarcodeWriter();
            writer.Format = BarcodeFormat.QR_CODE;
            QrCodeEncodingOptions options = new QrCodeEncodingOptions();
            // // Extended Channel Interpretation (ECI) 主要用于特殊的字符集。并不是所有的扫描器都支持这种编码。
            options.DisableECI = true;
            // 纠错级别
            options.ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.L;
            //设置内容编码
            options.CharacterSet = "UTF-8";
            //设置二维码的宽度和高度
            options.Width = 800;
            options.Height = 800;
            //设置二维码的边距,单位不是固定像素
            options.Margin = 1;
            writer.Options = options;

            Bitmap map = writer.Write(text);
            return map;
        }
    }

    /// <inheritdoc/>
    public class CustomFilterInfo : FilterInfo
    {
        private string _name;

        public CustomFilterInfo(string monikerString) : base(monikerString)
        {
        }

        public CustomFilterInfo(string monikerString, string name) : this(monikerString)
        {
            Name = name;
        }

        public new string Name
        {
            get => string.IsNullOrEmpty(_name) ? base.Name : _name; 
            private set => _name = value; 
        }

        public override string ToString()
        {
            return $"{Name} (C)";
        }

        public static CustomFilterInfo FromFilterInfo(FilterInfo filterInfo)
        {
            return new CustomFilterInfo(filterInfo.MonikerString);
        }
    }
}
