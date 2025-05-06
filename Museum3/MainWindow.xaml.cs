using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Configuration;
using System.Windows.Interop;

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int Columns = 4; // Количество колонок в строке
        public string RootPath = ConfigurationManager.AppSettings["MainFolder"] + @"\catalog";
        private List<string> _imagePaths = new List<string>();  // Список изображений
        private int _currentIndex = 0; // Текущий индекс изображения
        private Point _startPoint; // Начало свайпа
        private string[] categoryFolders; // Все категории
        private int currentPage = 0; // Текущая страница
        private const int ItemsPerPage = 8; // Элементов на страниц
        private DispatcherTimer idleTimer;
        private DispatcherTimer autoSlideTimer;
        private DispatcherTimer mainWindowHold;
        private TimeSpan idleThreshold = TimeSpan.FromMinutes(int.Parse(ConfigurationManager.AppSettings["MainSliderHold"]));
        private bool isAutoSliding = false;
        protected string mainFolder = ConfigurationManager.AppSettings["MainFolder"];
        private DispatcherTimer reverseSliderCheckTimer;


        public MainWindow()
        {
            InitializeComponent();
            autoSlideTimer = new DispatcherTimer();
            int mainSliderTimeout = int.Parse(ConfigurationManager.AppSettings["MainSliderInterval"]);
            autoSlideTimer.Interval = TimeSpan.FromSeconds(mainSliderTimeout); //Значение из конфига
            autoSlideTimer.Tick += (s, e) => SlideNextImage();
            LoadCategories(RootPath); // Укажи путь к папке с категориями
            Left.PlayVideo(mainFolder + @"\Sprites\Gears1.wmv");
            Right.PlayVideo(mainFolder + @"\Sprites\Gears2.wmv");
            LoadImages(RootPath);
            ShowImageWithFade();
            string videoPath = mainFolder + @"\Sprites\SliderButton.wmv";
            SliderButton.Source = new Uri(videoPath);
            string videoPathRevers = mainFolder + @"\Sprites\SliderButtonRevers.wmv";
            SliderButtonRevers.Source = new Uri(videoPathRevers);
            string videoButtonUp = mainFolder + @"\Sprites\ButtonUP.wmv";
            ButtonUp.Source = new Uri(videoButtonUp);
            string videoButtonDown = mainFolder + @"\Sprites\ButtonDown.wmv";
            ButtonDown.Source = new Uri(videoButtonDown);
            LoadCategoryButton();
            LoadSliderButton();
            InitializeIdleDetection();
            SetAllOpenButtonsEnabled(true);
        }

        private void LoadCategories(string rootPath)
        {
            if (!Directory.Exists(rootPath)) return;

            // Загружаем и сортируем категории
            categoryFolders = Directory.GetDirectories(rootPath)
                .OrderBy(f => ExtractNumber(f))
                .ToArray();

            // Показываем первую страницу
            DisplayPage();
            UpdatePageNavigationButtons();
        }

        // Функция показа текущей страницы
        private void DisplayPage(bool enableInteraction = true)
        {
            ContainersGrid.Children.Clear();
            ContainersGrid.RowDefinitions.Clear();
            ContainersGrid.ColumnDefinitions.Clear();

            int totalItems = categoryFolders.Length;
            int totalPages = (int)Math.Ceiling(totalItems / (double)ItemsPerPage);

            int startIndex;
            int endIndex;

            // Последняя страница — показываем последние 8
            if (currentPage == totalPages - 1 && totalItems > ItemsPerPage)
            {
                startIndex = Math.Max(0, totalItems - ItemsPerPage);
                endIndex = totalItems;
            }
            else
            {
                startIndex = currentPage * ItemsPerPage;
                endIndex = Math.Min(startIndex + ItemsPerPage, totalItems);
            }

            int itemsCount = endIndex - startIndex;
            int rows = (int)Math.Ceiling(itemsCount / (double)Columns);

            // Создаём строки и столбцы
            for (int i = 0; i < rows; i++)
                ContainersGrid.RowDefinitions.Add(new RowDefinition());

            for (int j = 0; j < Columns; j++)
                ContainersGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Добавляем категории в Grid
            for (int i = startIndex, pos = 0; i < endIndex; i++, pos++)
            {
                int row = pos / Columns;
                int col = pos % Columns;

                int positionOnScreen = pos; // от 0 до 7
                ContainerImage containerImage = new ContainerImage(positionOnScreen);
                containerImage.SetCategory(categoryFolders[i]);
                containerImage.SetOpenButtonEnabled(enableInteraction);

                Grid.SetRow(containerImage, row);
                Grid.SetColumn(containerImage, col);
                ContainersGrid.Children.Add(containerImage);
            }
            //Console.WriteLine($"Страница {currentPage + 1} из {totalPages}");
        }

        //Проверка первая или последняя страница
        private void UpdatePageNavigationButtons()
        {
            int totalPages = (int)Math.Ceiling(categoryFolders.Length / (double)ItemsPerPage);

            ButtonUp.IsEnabled = currentPage > 0;
            ButtonDown.IsEnabled = currentPage < totalPages - 1;
        }

        //Проходимся по всем дочерним элементам
        public async Task PartialExpandAllDescriptions()
        {
            List<Task> expandTasks = new List<Task>();

            foreach (var child in ContainersGrid.Children)
            {
                if (child is ContainerImage container)
                {
                    expandTasks.Add(container.ResetDesc());
                    expandTasks.Add(container.PartialExpandDesc());
                }
            }

            await Task.WhenAll(expandTasks);
        }
        public async Task PartialExpandAllDescriptionsReset()
        {
            List<Task> expandTasks = new List<Task>();

            foreach (var child in ContainersGrid.Children)
            {
                if (child is ContainerImage container)
                {
                    expandTasks.Add(container.PartialExpandDescReset());
                }
            }

            await Task.WhenAll(expandTasks);
        }

        // Извлекаем число из строки
        static int ExtractNumber(string folderName)
        {
            Match match = Regex.Match(folderName, @"\d+"); // Ищем число
            return match.Success ? int.Parse(match.Value) : int.MaxValue; // Если нашли, парсим, иначе ставим макс. значение
        }

        // Загружаем изображения из всех подкаталогов
        private void LoadImages(string rootFolder)
        {
            if (Directory.Exists(rootFolder))
            {
                _imagePaths = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories)
                                       .Where(f => f.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase) ||
                                                   f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                   f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                   f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                       .ToList();
            }
        }

        // Показываем изображение с анимацией
        private void ShowImageWithFade()
        {
            if (_imagePaths.Count > 0 && _currentIndex >= 0 && _currentIndex < _imagePaths.Count)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                fadeOut.Completed += (s, e) =>
                {
                    // Меняем изображение, когда текущее полностью исчезло
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_imagePaths[_currentIndex]);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    SliderFoto.Source = bitmap;

                    // Плавно показываем новое изображение
                    DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    SliderFoto.BeginAnimation(OpacityProperty, fadeIn);
                };

                // Запускаем анимацию затухания
                SliderFoto.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void InitializeIdleDetection()
        {
            // Таймер бездействия
            idleTimer = new DispatcherTimer
            {
                Interval = idleThreshold
            };
            idleTimer.Tick += (s, e) =>
            {
                StartAutoSlide(); // запускаем автослайд при простое
            };

            // Таймер автослайда
            autoSlideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["MainSliderInterval"])) // Значение из конфига
            };
            autoSlideTimer.Tick += (s, e) => SlideNextImage();

            // Новый таймер для проверки и опускания слайдера
            reverseSliderCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(int.Parse(ConfigurationManager.AppSettings["MainWindowHold"])) 
            };
            reverseSliderCheckTimer.Tick += (s, e) =>
            {
                CheckAndReverseSlider();
            };

            // Подключаем отслеживание активности
            this.PreviewMouseDown += ResetIdleTimer;
            this.PreviewTouchDown += ResetIdleTimer;
            this.PreviewKeyDown += ResetIdleTimer;

            idleTimer.Start(); // Запускаем таймер отслеживания
        }

        private void ResetIdleTimer(object sender, EventArgs e)
        {
            idleTimer.Stop();
            idleTimer.Start();

            if (isAutoSliding)
            {
                StopAutoSlide();
            }
        }

        private void StartAutoSlide()
        {
            if (!isAutoSliding)
            {
                isAutoSliding = true;
                autoSlideTimer.Start();
                reverseSliderCheckTimer.Start(); // запускаем таймер проверки слайдера
            }
        }

        private void StopAutoSlide()
        {
            isAutoSliding = false;
            autoSlideTimer.Stop();
            reverseSliderCheckTimer.Stop(); // останавливаем таймер проверк
        }

        private void CheckAndReverseSlider()
        {
            double sliderTop = Canvas.GetTop(SliderFrame);

            // Если слайдер не опущен
            if (sliderTop == -730) 
            {
                SliderButtonRevers_MouseDown(null, null);
            }

            reverseSliderCheckTimer.Stop(); // выполняется один раз после начала бездействия
        }

        private void SlideNextImage()
        {
            if (_imagePaths.Count == 0) return;

            _currentIndex = (_currentIndex + 1) % _imagePaths.Count;
            ShowImageWithFade();
        }


        // Запоминаем точку начала свайпа
        private void MainImage_MouseDownNoTouch(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Остановить распространение события
        }
        private void MainImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            e.Handled = true; // Остановить распространение события
        }

        // Обрабатываем свайп + анимация затухания/проявления
        private void MainImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point endPoint = e.GetPosition(this);
            double deltaX = endPoint.X - _startPoint.X;

            if (Math.Abs(deltaX) > 50) // Минимальная длина свайпа в 50px
            {
                if (deltaX < 0 && _currentIndex < _imagePaths.Count - 1) // Свайп влево
                {
                    _currentIndex++;
                    ShowImageWithFade();
                }
                else if (deltaX > 0 && _currentIndex > 0) // Свайп вправо
                {
                    _currentIndex--;
                    ShowImageWithFade();
                }
            }
        }

        //Загружаем и стопим на первом кадре кнопку
        public async Task LoadAlbumUp(string videoAlbumUp)
        {
            AlbumUp.Source = new Uri(videoAlbumUp);
            AlbumUp.Opacity = 1;
            AlbumUp.Position = TimeSpan.Zero;
            AlbumUp.Play();           // ТОЛЬКО чтобы инициировать отрисовку
            //AlbumUp.Pause();          // Остановим, чтобы не играть пока

            await Dispatcher.Yield(DispatcherPriority.Render); // дожидаемся отрисовки

            await AnimateOpacityAsync(AlbumUp, 0, 1, TimeSpan.FromSeconds(0.3));
            
            //AlbumUp.Play(); // теперь уже настоящий запуск

        }
        private Task AnimateOpacityAsync(UIElement element, double from, double to, TimeSpan duration)
        {
            var tcs = new TaskCompletionSource<bool>();

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (s, e) => tcs.TrySetResult(true);

            element.BeginAnimation(UIElement.OpacityProperty, animation);

            return tcs.Task;
        }

        private void AlbumUp_MediaEnded(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3), // длительность исчезновения
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (s, a) =>
            {
                AlbumUp.Opacity = 0; // окончательно скрываем
                AlbumUp.Stop();

                // ОСВОБОЖДАЕМ ПАМЯТЬ
                AlbumUp.Source = null;

                // Если метод Close доступен (не всегда)
                try
                {
                    AlbumUp.Close(); // В некоторых сборках доступно, в других — проигнорируется
                }
                catch { }
            };

            AlbumUp.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void LoadSliderButton()
        {
            SliderButton.Visibility = Visibility.Hidden;
            SliderButton.Play();
            //SliderButtonRevers.Position = TimeSpan.Zero;
            SliderButtonRevers.Visibility = Visibility.Visible;
            SliderButtonRevers.Play();
        }

        private void LoadCategoryButton()
        {
            ButtonUp.Position = TimeSpan.Zero;
            ButtonDown.Position = TimeSpan.Zero;
            ButtonUp.Play();
            ButtonDown.Play();
        }

        //Блокировка всех кнопок открыть
        public void SetAllOpenButtonsEnabled(bool isEnabled)
        {
            foreach (var child in ContainersGrid.Children)
            {
                if (child is ContainerImage container)
                {
                    container.SetOpenButtonEnabled(isEnabled);
                }
            }
        }

        //Следующий набор категорий
        public async void NextPage_Click(object sender, MouseButtonEventArgs e)
        {
            SetAllOpenButtonsEnabled(false);
            SliderButton.IsEnabled = false;
            SliderButtonRevers.IsEnabled = false;
            ButtonUp.IsEnabled = false;
            ButtonDown.IsEnabled = false;
            ButtonDown.Position = TimeSpan.Zero;
            ButtonDown.SpeedRatio = 2;
            ButtonDown.Play();

            if (Canvas.GetTop(SliderFrame) == 100)
            {
                await AnimateUpSlider();
            }

            AnimatePlank();
            await PartialExpandAllDescriptions();
            string videoAlbumUp = mainFolder + @"\Sprites\AlbumUp_1080.wmv";
            await LoadAlbumUp(videoAlbumUp);
            int totalPages = (int)Math.Ceiling(categoryFolders.Length / (double)ItemsPerPage);
            if (currentPage < totalPages - 1)
            {
                await Task.Delay(900);
                currentPage++;
                DisplayPage(false);
                await PartialExpandAllDescriptions();
            }
            await Task.Delay(4100);
            await PartialExpandAllDescriptionsReset();
            await AnimatePlankReset();
            await Task.Delay(100);
            SliderButton.IsEnabled = true;
            SliderButtonRevers.IsEnabled = true;
            ButtonUp.IsEnabled = true;
            ButtonDown.IsEnabled = true;
            UpdatePageNavigationButtons();
            SetAllOpenButtonsEnabled(true);
        }

        //Предыдущий набор категорий
        private async void PrevPage_Click(object sender, MouseButtonEventArgs e)
        {
            SetAllOpenButtonsEnabled(false);
            SliderButton.IsEnabled = false;
            SliderButtonRevers.IsEnabled = false;
            ButtonUp.IsEnabled = false;
            ButtonDown.IsEnabled = false;
            ButtonUp.Position = TimeSpan.Zero;
            ButtonUp.SpeedRatio = 2;
            ButtonUp.Play();

            if (Canvas.GetTop(SliderFrame) == 100)
            {
                await AnimateUpSlider();
            }

            AnimatePlank();
            await PartialExpandAllDescriptions();
            string videoAlbumUp = mainFolder + @"\Sprites\AlbumDown_1080.wmv";
            await LoadAlbumUp(videoAlbumUp);
            if (currentPage > 0)
            {
                await Task.Delay(300);
                currentPage--;
                DisplayPage(false);
                await PartialExpandAllDescriptions();
            }
            await Task.Delay(4700);
            await PartialExpandAllDescriptionsReset();
            await AnimatePlankReset();
            await Task.Delay(100);
            SliderButton.IsEnabled = true;
            SliderButtonRevers.IsEnabled = true;
            ButtonUp.IsEnabled = true;
            ButtonDown.IsEnabled = true;
            UpdatePageNavigationButtons();
            SetAllOpenButtonsEnabled(true);
        }

        //Анимируем планки пролистывания страниц
        public void AnimatePlank()
        {
            double startPosLeftPlank = Canvas.GetLeft(LeftPlank1);
            double startPosRightPlank = Canvas.GetLeft(RightPlank1);
            double newPosLeftPlank = Canvas.GetLeft(LeftPlank1) + 770;
            double newPosRightPlank = Canvas.GetLeft(RightPlank1) - 758;

            DoubleAnimation LeftPlank = new DoubleAnimation
            {
                To = newPosLeftPlank,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };
            DoubleAnimation RightPlank = new DoubleAnimation
            {
                To = newPosRightPlank,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            LeftPlank1.BeginAnimation(Canvas.LeftProperty, LeftPlank);
            LeftPlank2.BeginAnimation(Canvas.LeftProperty, LeftPlank);
            RightPlank1.BeginAnimation(Canvas.LeftProperty, RightPlank);
            RightPlank2.BeginAnimation(Canvas.LeftProperty, RightPlank);
        }
        public async Task AnimatePlankReset()
        {
            double newPosLeftPlank = Canvas.GetLeft(LeftPlank1) - 770;
            double newPosRightPlank = Canvas.GetLeft(RightPlank1) + 758;

            var tcs = new TaskCompletionSource<bool>();
            int completed = 0;

            void CheckComplete()
            {
                if (++completed >= 2)
                    tcs.TrySetResult(true);
            }

            DoubleAnimation LeftPlank = new DoubleAnimation
            {
                To = newPosLeftPlank,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            LeftPlank.Completed += (s, e) => CheckComplete();

            DoubleAnimation RightPlank = new DoubleAnimation
            {
                To = newPosRightPlank,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            RightPlank.Completed += (s, e) => CheckComplete();

            LeftPlank1.BeginAnimation(Canvas.LeftProperty, LeftPlank);
            LeftPlank2.BeginAnimation(Canvas.LeftProperty, LeftPlank);
            RightPlank1.BeginAnimation(Canvas.LeftProperty, RightPlank);
            RightPlank2.BeginAnimation(Canvas.LeftProperty, RightPlank);

            await tcs.Task;
        }


        public async Task AnimateUpSlider()
        {
            
            SliderButtonRevers.Position = TimeSpan.Zero;
            SliderButtonRevers.Visibility = Visibility.Visible;
            SliderButtonRevers.IsHitTestVisible = false;
            await Task.Delay(50); // заменяем Thread.Sleep на await
            SliderButton.Visibility = Visibility.Hidden;
            SliderButtonRevers.Play();

            double newTopFrameSlider = Canvas.GetTop(SliderFrame) - 830;
            double newTopFotoSlider = Canvas.GetTop(SliderFoto) - 845;
            double newTopFotoBackSlider = Canvas.GetTop(SliderFotoBack) - 845;

            var tcs = new TaskCompletionSource<bool>();

            DoubleAnimation sliderFrameAnim = new DoubleAnimation
            {
                To = newTopFrameSlider,
                Duration = TimeSpan.FromSeconds(2.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation sliderFotoAnim = new DoubleAnimation
            {
                To = newTopFotoSlider,
                Duration = TimeSpan.FromSeconds(2.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation sliderFotoBackAnim = new DoubleAnimation
            {
                To = newTopFotoBackSlider,
                Duration = TimeSpan.FromSeconds(2.6),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            SliderFotoBack.IsHitTestVisible = false;
            SliderFoto.IsHitTestVisible = false;
            BackSlider.IsHitTestVisible = false;
            //BackSlider.Background = new SolidColorBrush(Colors.Transparent);

            sliderFotoAnim.Completed += (s, a) =>
            {
                SliderButtonRevers.IsHitTestVisible = true;
                tcs.TrySetResult(true); // уведомляем, что анимация завершена
            };

            SliderFoto.BeginAnimation(Canvas.TopProperty, sliderFotoAnim);
            SliderFotoBack.BeginAnimation(Canvas.TopProperty, sliderFotoBackAnim);
            SliderFrame.BeginAnimation(Canvas.TopProperty, sliderFrameAnim);

            await tcs.Task; // ждём окончания анимации
        }


        //Переключение тумблера слайдера
        private async void SliderButton_MouseDown(object sender, RoutedEventArgs e)
        {
            SetAllOpenButtonsEnabled(false);
            ButtonUp.IsEnabled = false;
            ButtonDown.IsEnabled = false;
            await AnimateUpSlider();
            ButtonUp.IsEnabled = true;
            ButtonDown.IsEnabled = true;
            SetAllOpenButtonsEnabled(true);
        }

        private void SliderButtonRevers_MouseDown(object sender, RoutedEventArgs e)
        {
            SetAllOpenButtonsEnabled(false);
            ButtonUp.IsEnabled = false;
            ButtonDown.IsEnabled = false;
            SliderButton.Position = TimeSpan.Zero;
            SliderButton.Visibility = Visibility.Visible;
            SliderButton.IsHitTestVisible = false;
            Thread.Sleep(30);
            SliderButtonRevers.Visibility = Visibility.Hidden;
            
            SliderButton.Play();

            //Анимация опускания слайдера
            double startPosFrameSlider = Canvas.GetTop(SliderFrame);
            double newTopFrameSlider = Canvas.GetTop(SliderFrame) + 830;

            DoubleAnimation sliderFrameAnim = new DoubleAnimation
            {
                To = newTopFrameSlider,
                Duration = TimeSpan.FromSeconds(2.3),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Анимация опускания фото
            double startPosFotoSlider = Canvas.GetTop(SliderFoto);
            double newTopFotoSlider = Canvas.GetTop(SliderFoto) + 845;
            double newTopFotoSliderBack = Canvas.GetTop(SliderFotoBack) + 845;

            DoubleAnimation sliderFotoAnim = new DoubleAnimation
            {
                To = newTopFotoSlider,
                Duration = TimeSpan.FromSeconds(2.3),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            DoubleAnimation sliderFotoBackAnim = new DoubleAnimation
            {
                To = newTopFotoSliderBack,
                Duration = TimeSpan.FromSeconds(2.3),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //sliderFotoAnim.Completed += (s, a) => BackSlider.Background = new SolidColorBrush(Colors.Black);
            sliderFotoAnim.Completed += (s, a) => SliderFotoBack.IsHitTestVisible = true;
            sliderFotoAnim.Completed += (s, a) => SliderFoto.IsHitTestVisible = true;
            sliderFotoAnim.Completed += (s, a) => BackSlider.IsHitTestVisible = true;
            sliderFotoAnim.Completed += (s, a) => SliderButton.IsHitTestVisible = true;
            sliderFotoAnim.Completed += (s, a) => ButtonUp.IsEnabled = true;
            sliderFotoAnim.Completed += (s, a) => ButtonDown.IsEnabled = true;
            sliderFotoAnim.Completed += (s, a) => SetAllOpenButtonsEnabled(true);

            SliderFoto.BeginAnimation(Canvas.TopProperty, sliderFotoAnim);
            SliderFotoBack.BeginAnimation(Canvas.TopProperty, sliderFotoBackAnim);
            SliderFrame.BeginAnimation(Canvas.TopProperty, sliderFrameAnim);
        }

        public void ButtonDisable()
        {
            ButtonUp.IsHitTestVisible = false;
            ButtonDown.IsHitTestVisible = false;
            SliderButton.IsHitTestVisible = false;
            SliderButtonRevers.IsHitTestVisible = false;
            PassWindow.IsHitTestVisible = false;
        }

        public void ButtonEnsable()
        {
            ButtonUp.IsHitTestVisible = true;
            ButtonDown.IsHitTestVisible = true;
            SliderButton.IsHitTestVisible = true;
            SliderButtonRevers.IsHitTestVisible = true;
            PassWindow.IsHitTestVisible = true;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            //this.Content = null;
            //this.Close();
            string password = ShowTouchPasswordDialog();
            string passFromConfig = ConfigurationManager.AppSettings["Password"];

            if (password == passFromConfig)
            {
                this.Content = null;
                this.Close();
            }
            else if (password != null)
            {
                
            }
        }

        //Ввод пароля
        public static string ShowTouchPasswordDialog(string title = "Введите пароль")
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Owner = Application.Current.MainWindow,
                Topmost = true
            };

            var password = new StringBuilder();
            var passwordDisplay = new TextBlock
            {
                Text = "",
                FontSize = 24,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };

            // Обновление отображения пароля
            void UpdateDisplay()
            {
                passwordDisplay.Text = new string('●', password.Length);
            }

            var grid = new UniformGrid
            {
                Columns = 3,
                Rows = 4,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Кнопки 1-9
            for (int i = 1; i <= 9; i++)
            {
                int digit = i;
                var btn = new Button
                {
                    Content = digit.ToString(),
                    FontSize = 24,
                    Margin = new Thickness(5),
                    Height = 50,
                    Width = 50
                };
                btn.Click += (s, e) =>
                {
                    password.Append(digit);
                    UpdateDisplay();
                };
                grid.Children.Add(btn);
            }

            // Кнопка "Стереть"
            var deleteBtn = new Button
            {
                Content = "←",
                FontSize = 24,
                Margin = new Thickness(5),
                Height = 50
            };
            deleteBtn.Click += (s, e) =>
            {
                if (password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    UpdateDisplay();
                }
            };
            grid.Children.Add(deleteBtn);

            // Кнопка 0
            var zeroBtn = new Button
            {
                Content = "0",
                FontSize = 24,
                Margin = new Thickness(5),
                Height = 50
            };
            zeroBtn.Click += (s, e) =>
            {
                password.Append("0");
                UpdateDisplay();
            };
            grid.Children.Add(zeroBtn);

            // Пустая кнопка (для выравнивания)
            grid.Children.Add(new TextBlock());

            // Кнопки OK и Отмена
            var okBtn = new Button
            {
                Content = "OK",
                FontSize = 18,
                Width = 100,
                Margin = new Thickness(10)
            };
            var cancelBtn = new Button
            {
                Content = "Отмена",
                FontSize = 18,
                Width = 100,
                Margin = new Thickness(10)
            };

            string result = null;
            okBtn.Click += (s, e) =>
            {
                result = password.ToString();
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelBtn.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { okBtn, cancelBtn }
            };

            // создаём изображение как фон
            string mainFolder = ConfigurationManager.AppSettings["MainFolder"];
            var backgroundImage = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(mainFolder + @"\Sprites\fonDesc.png", UriKind.Absolute)),
                Stretch = Stretch.UniformToFill // можно поменять на Uniform или UniformToFill
            };

            var layout = new StackPanel();
            layout.Children.Add(passwordDisplay);
            layout.Children.Add(grid);
            layout.Children.Add(buttonPanel);

            dialog.Content = layout;
            dialog.Background = backgroundImage;
            dialog.ShowDialog();

            return result;
        }

    }
}
