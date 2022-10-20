using Microsoft.Win32;
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
        private List<CustomFilterInfo> _cameras;
        private string _imageName;

        public MainWindow()
        {
            InitializeComponent();

            this.Width = AppOptions.Width;
            this.Height = AppOptions.Height;
            LeftGrid.Width = new GridLength(AppOptions.LeftGridRatio, GridUnitType.Star);
            RightGrid.Width = new GridLength(AppOptions.RightGridRatio, GridUnitType.Star);
            UpGrid.Height = new GridLength(AppOptions.UpGridRatio, GridUnitType.Star);
            DownGrid.Height = new GridLength(AppOptions.DownGridRatio, GridUnitType.Star);

            _qrHelper = new QrHelper();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _cameras = _qrHelper.GetFilterInfo();
            _cameras.Insert(0, new CustomFilterInfo("", "请选择一个摄像头"));
            CameraComboBox.ItemsSource = _cameras;
            CameraComboBox.SelectedIndex = 0;
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cameras.Count > 1 && (sender is ComboBox comboBox) && comboBox.SelectedIndex > 0)
            {
                videoSourcePlayer.VideoSource = _qrHelper.OpenDevice(_cameras[comboBox.SelectedIndex]);
                videoSourcePlayer.Start();

                ShotButton.IsEnabled = true;//开启“拍摄功能”
                AutoRecognizeChectBox.IsEnabled = true;
            }
            else
            {
                _qrHelper.CloseVideoSource();

                ShotButton.IsEnabled = false;
                AutoRecognizeChectBox.IsEnabled = false;
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
                _imageName = $"{AppOptions.AppDataFolder}/{DateTime.Now:yyyyMMdd_HHmmss_ffff}.jpg";
                img.Save(_imageName, ImageFormat.Jpeg);
                RecognizeButton.IsEnabled = true;
            }
        }

        private async void AutoRecognizeChectBox_Checked(object sender, RoutedEventArgs e)
        {
            RecognizeButton.IsEnabled = false;
            SelectButton.IsEnabled = false;
            GenerateQrButton.IsEnabled = false;
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
            GenerateQrButton.IsEnabled = true;
        }

        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            var codeString = _qrHelper.ScanBarcode(_imageName);
            ShowCodeString(codeString);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            AppOptions.Width = this.Width;
            AppOptions.Height = this.Height;
            AppOptions.LeftGridRatio = LeftGrid.Width.Value;
            AppOptions.RightGridRatio = RightGrid.Width.Value;
            AppOptions.UpGridRatio = UpGrid.Height.Value;
            AppOptions.DownGridRatio = DownGrid.Height.Value;
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

        private void GenerateQrButton_Click(object sender, RoutedEventArgs e)
        {
            var text = MessageBlock.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请先在文本框内输入文本");
                return;
            }
            var map = _qrHelper.GenerateQr(MessageBlock.Text);
            ImageArea.Source = map.ToBitmapImage();
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (MessageBlock.IsKeyboardFocused)
            {
                if (e.Key == Key.Escape)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
                return;
            }
            switch (e.Key)
            {
                case Key.C:
                    CameraComboBox.IsDropDownOpen = true;
                    e.Handled = true;
                    break;
                case Key.P:
                    SelectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
                case Key.S:
                    ShotButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
                case Key.G:
                    GenerateQrButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
                case Key.A:
                    AutoRecognizeChectBox.IsChecked = !AutoRecognizeChectBox.IsChecked;
                    e.Handled = true;
                    break;
                case Key.R:
                    RecognizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void ImageArea_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg | All Files | *.*";
            sfd.RestoreDirectory = true;//保存对话框是否记忆上次打开的目录 
            sfd.Title = "将图片另存为选定的文件";
            sfd.FileName = $"PcQrReader{DateTime.Now:yyyyMMddHHmmss}.png";
            if (sfd.ShowDialog() == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(this.ImageArea.Source as BitmapSource));
                using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }
    }
}
