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
            if (AppOptions.Left <= 0 || AppOptions.Top <= 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                this.Left = AppOptions.Left;
                this.Top = AppOptions.Top;
            }
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
            dialog.Filter = $"图像文件({string.Join(",", AppOptions.PictureSuffix)})|{string.Join(";", AppOptions.PictureSuffix)}|所有文件|*.*";
            // 打开选择框选择
            var result = dialog.ShowDialog();
            if (result == true)
            {
                _imageName = dialog.FileName; // 获取选择的文件名
                ShowImage(_imageName);
            }
        }

        private void ShowImage(string imageName)
        {
            ImageArea.Source = new BitmapImage(new Uri(imageName, UriKind.Absolute));
            RecognizeButton.IsEnabled = true;
        }

        private void ImageArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ImageArea_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                // 快捷方式需要获取目标文件路径
                if (fileName.ToLower().EndsWith("lnk"))
                {
                    var shell = new IWshRuntimeLibrary.WshShell();
                    var wshShortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(fileName);
                    fileName = wshShortcut.TargetPath;
                }
                var extension = System.IO.Path.GetExtension(fileName);
                if (!AppOptions.PictureSuffix.Any(s => s.EndsWith(extension, StringComparison.CurrentCultureIgnoreCase)))
                {
                    var message = $"该文件后缀为 {extension}，似乎不是图片，确定要拖入吗？";
                    if (MessageBox.Show(message, "确认窗口", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                _imageName = fileName;
                ShowImage(_imageName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                _imageName = GetNewImageName();
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
                        _imageName = GetNewImageName();
                        img.Save(_imageName);
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
            AppOptions.Left = this.Left;
            AppOptions.Top = this.Top;
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

        private static string GetNewImageName()
        {
            return $"{AppOptions.AppDataFolder}/{AppOptions.TempFileSuffix}{DateTime.Now:yyyyMMdd_HHmmss_ffff}.jpg";
        }
    }
}
