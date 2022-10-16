using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using ZXing;
using ZXing.Common;

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
        public CustomFilterInfo[] GetFilterInfo()
        {
            CustomFilterInfo[] filterInfos;
            try
            {
                //AForge.Video.DirectShow.FilterInfoCollection 设备枚举类
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_videoDevices.Count == 0)
                    return null;
                filterInfos = new CustomFilterInfo[_videoDevices.Count];
                for (int i = 0; i < _videoDevices.Count; i++)
                {
                    filterInfos[i] = CustomFilterInfo.FromFilterInfo(_videoDevices[i]);
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
            DecodingOptions decodeOption = new DecodingOptions();
            decodeOption.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };

            //读取操作
            BarcodeReader reader = new BarcodeReader();
            reader.Options = decodeOption;
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

        public void Dispose()
        {
            CloseVideoSource(ref _videoCaptureDevice_on);
        }
    }

    /// <inheritdoc/>
    public class CustomFilterInfo : FilterInfo
    {
        public CustomFilterInfo(string monikerString) : base(monikerString)
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public static CustomFilterInfo FromFilterInfo(FilterInfo filterInfo)
        {
            return new CustomFilterInfo(filterInfo.MonikerString);
        }
    }
}
