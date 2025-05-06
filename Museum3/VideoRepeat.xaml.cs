using System;
using System.Collections.Generic;
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

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для VideoRepeat.xaml
    /// </summary>
    public partial class VideoRepeat : UserControl
    {

        public VideoRepeat()
        {
            InitializeComponent();
        }

        public void PlayVideo(string videoPath)
        {
            if (System.IO.File.Exists(videoPath))
            {
                VideoPlayer.Source = new Uri(videoPath, UriKind.RelativeOrAbsolute);
                VideoPlayer.Play();
            }
            else
            {
                MessageBox.Show("Файл видео не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position = TimeSpan.Zero; // Перемотка
            VideoPlayer.Play();
        }
    }
}
