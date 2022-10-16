using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PcQrReader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private QrHelper _qrHelper;
        private CustomFilterInfo[] _cameras;
        private string _imageName;

        public MainWindow()
        {
            InitializeComponent();

            _qrHelper = new QrHelper();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _cameras = _qrHelper.GetFilterInfo();
            CameraComboBox.ItemsSource = _cameras;
            if (_cameras != null)
            {
                CameraComboBox.SelectedIndex = 0;
            }
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cameras.Length > 0 && (sender is ComboBox comboBox) && comboBox.SelectedIndex > -1)
            {
                videoSourcePlayer.VideoSource = _qrHelper.OpenDevice(_cameras[comboBox.SelectedIndex]);
                videoSourcePlayer.Start();

                ShotButton.IsEnabled = true;//开启“拍摄功能”
                AutoRecognizeChectBox.IsEnabled = true;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            // 实例化一个文件选择对象
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".png";  // 设置默认类型
                                         // 设置可选格式
            dialog.Filter = @"图像文件(*.jpg,*.png,*.tif,*.gif)|*jpeg;*.jpg;*.png;*.tif;*.tiff;*.gif
      |JPEG(*.jpeg, *.jpg)|*.jpeg;*.jpg|PNG(*.png)|*.png|GIF(*.gif)|*.gif
      |TIF(*.tif,*.tiff)|*.tif;*.tiff";
            // 打开选择框选择
            var result = dialog.ShowDialog();
            if (result == true)
            {
                _imageName = dialog.FileName; // 获取选择的文件名
                ImageArea.Source = new BitmapImage(new Uri(_imageName, UriKind.Absolute));
                RecognizeButton.IsEnabled = true;
            }
        }

        private void ShotButton_Click(object sender, RoutedEventArgs e)
        {
            using (var img = videoSourcePlayer.GetCurrentVideoFrame())//拍摄
            {
                if (AutoRecognizeChectBox.IsChecked != true)
                {
                    ImageArea.Source = img.ToBitmapImage();
                }
                Directory.CreateDirectory("Photos");
                _imageName = $"Photos/{DateTime.Now:yyyyMMdd_HHmmss_ffff}.jpg";
                img.Save(_imageName, ImageFormat.Jpeg);
                RecognizeButton.IsEnabled = true;
            }
        }

        private async void AutoRecognizeChectBox_Checked(object sender, RoutedEventArgs e)
        {
            RecognizeButton.IsEnabled = false;
            SelectButton.IsEnabled = false;
            while (AutoRecognizeChectBox.IsChecked == true)
            {
                using (var img = videoSourcePlayer.GetCurrentVideoFrame())
                {
                    ImageArea.Source = img.ToBitmapImage();
                    var codeString = _qrHelper.ScanBarcode(img);
                    if (ShowCodeString(codeString))
                    {
                        break;
                    }
                }
                await Task.Delay(10);
            }
        }

        private void AutoRecognizeChectBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RecognizeButton.IsEnabled = true;
            SelectButton.IsEnabled = true;
        }

        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            var codeString = _qrHelper.ScanBarcode(_imageName);
            ShowCodeString(codeString);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private bool ShowCodeString(string codeString)
        {
            if (string.IsNullOrEmpty(codeString))
            {
                MessageBlock.Foreground = Brushes.Red;
                MessageBlock.Text = "未识别到二维码";
                return false;
            }
            else
            {
                MessageBlock.Foreground = Brushes.Black;
                MessageBlock.Text = codeString;
                AutoRecognizeChectBox.IsChecked = false;
                return true;
            }
        }
    }
}
