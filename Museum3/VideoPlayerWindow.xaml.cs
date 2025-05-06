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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для VideoPlayerWindow.xaml
    /// </summary>
    public partial class VideoPlayerWindow : Window
    {
        private bool readyToShow = false;
        public event EventHandler MediaEnded;

        public VideoPlayerWindow(string videoPath)
        {
            InitializeComponent();

            // Загружаем, но пока не показываем
            this.Loaded += (s, e) =>
            {
                VideoPlayer.Source = new Uri(videoPath, UriKind.Absolute);
                VideoPlayer.MediaOpened += VideoPlayer_MediaOpened;
                VideoPlayer.MediaEnded += VideoPlayer_MediaEnded;

                VideoPlayer.LoadedBehavior = MediaState.Manual;
                VideoPlayer.UnloadedBehavior = MediaState.Stop;

                VideoPlayer.Play(); // это начнёт загрузку
                VideoPlayer.Pause(); // подгружаем кадры
            };
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (readyToShow) return;
            readyToShow = true;

            // Показываем плавно
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                FillBehavior = FillBehavior.HoldEnd
            };

            fade.Completed += (s, a) =>
            {
                // 2. Только после fade запускаем видео
                VideoPlayer.Position = TimeSpan.Zero;
                VideoPlayer.Play();
            };

            this.BeginAnimation(Window.OpacityProperty, fade);
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }
        public async Task CloseWithFadeOut()
        {
            var tcs = new TaskCompletionSource<bool>();

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0.3,
                Duration = TimeSpan.FromMilliseconds(100),
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (s, e) => tcs.SetResult(true);
            this.BeginAnimation(Window.OpacityProperty, fadeOut);

            await tcs.Task;

            this.Close();
        }

    }
}
