using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using OfficeOpenXml;
using ExcelDataReader;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives; // для Track
using System.Configuration;
using System.Windows.Interop;

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для GalleryWindow.xaml
    /// </summary>
    public partial class GalleryWindow : Window
    {
        private DispatcherTimer inactivityTimer;
        private TimeSpan inactivityThreshold = TimeSpan.FromMinutes(int.Parse(ConfigurationManager.AppSettings["GalleryWindowHold"]));
        protected string mainFolder = ConfigurationManager.AppSettings["MainFolder"];
        private string[] paths = Array.Empty<string>();
        private int currentIndex = 4;
        private ObservableCollection<string> imagePaths = new ObservableCollection<string>(); // Оригинальные пути
        //private ObservableCollection<string> extendedPaths = new ObservableCollection<string>(); // Список с клонами
        private ObservableCollection<ImageItem> extendedPaths = new ObservableCollection<ImageItem>();
        private double scrollOffset = 0; // Смещение списка
        private const double ImageHeight = 74; // Высота одной картинки
        private const int MaxVisibleItems = 8; // Количество видимых изображений
        private bool isAnimating = false; // Флаг анимации
        private int indexFirstFoto = 9;
        private int indexSecondFoto = 15;
        private Dictionary<string, Dictionary<string, string>> imageDescriptions = new Dictionary<string, Dictionary<string, string>>();
        private string currentCategory;
        private bool isFullScreen = false;
        private Point origin;
        private Point start;
        private bool isDragging = false;
        private Point swipeStartPoint;
        private bool imageChange = false;
        private VideoPlayerWindow player;

        private ScaleTransform scaleTransform = new ScaleTransform(1, 1);
        private TranslateTransform translateTransform = new TranslateTransform();

        private DispatcherTimer loopTimer;
        private bool isLooping = false;
        private bool scrollingForward = true;

        public GalleryWindow(string categoryPath, VideoPlayerWindow playerWindow)
        {

            player = playerWindow;

            currentCategory = System.IO.Path.GetFileName(categoryPath); // Например: "category1"
            LoadDescriptions(mainFolder + @"\catalog\descImage.xlsx");

            this.Opacity = 0;
            InitializeComponent();
            // Настройка трансформации
            var group = new TransformGroup();
            group.Children.Add(scaleTransform);
            group.Children.Add(translateTransform);
            ImageFullScreen.RenderTransform = group;
            ImageFullScreen.RenderTransformOrigin = new Point(0.5, 0.5);

            loopTimer = new DispatcherTimer();
            loopTimer.Tick += LoopTimer_Tick;
            loopTimer.Interval = TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["GallerySliderInterval"])); //Значение из конфига

            LoadImages(categoryPath);

            GearsIdle.MediaOpened += (s, e) =>
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            };

            GearsIdle_Background(mainFolder + @"\Sprites\Gears_Idle.wmv");
            string videoButtonNext = mainFolder + @"\Sprites\Button_prev.wmv";
            ButtonNext.Source = new Uri(videoButtonNext);
            string videoButtonPrev = mainFolder + @"\Sprites\Button_next.wmv";
            ButtonPrev.Source = new Uri(videoButtonPrev);
            LoadButton();
            SliderContainer.ItemsSource = extendedPaths; // Привязываем пути к списку
            _ = RevealItemsSequentially(); // запускаем поочерёдную загрузку
            NextImage_Click(null, null);
            MoveNthItemRight(indexFirstFoto);
            StartImageDisplayAnim();
            TextBlockDescImage.FontSize = int.Parse(ConfigurationManager.AppSettings["GalleryDescFontSize"]);
            LoadCategoryDisplayName(currentCategory);

            inactivityTimer = new DispatcherTimer();
            inactivityTimer.Interval = inactivityThreshold;
            inactivityTimer.Tick += InactivityTimer_Tick;
            inactivityTimer.Start();

            // События взаимодействия
            this.AddHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>((s, e) => ResetInactivityTimer()), true);
            this.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler((s, e) => ResetInactivityTimer()), true);
            this.AddHandler(UIElement.StylusDownEvent, new StylusDownEventHandler((s, e) => ResetInactivityTimer()), true);
        }

        //Логика таймера
        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            CloseWindow_Click(this, null);
        }

        private void ResetInactivityTimer()
        {
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }


        //Название категории
        private void LoadCategoryDisplayName(string categoryId)
        {
            string excelPath = mainFolder + @"\catalog\descCategory.xlsx";

            if (!File.Exists(excelPath))
            {
                MessageBox.Show("Файл descCategory.xlsx не найден!");
                return;
            }

            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(excelPath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    int rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        string id = worksheet.Cells[row, 1].Text; // A - ID
                        string name = worksheet.Cells[row, 3].Text; // C - Display Name

                        if (id.Equals(categoryId, StringComparison.OrdinalIgnoreCase))
                        {
                            NameCategory.Text = name;
                            NameCategory.TextWrapping = TextWrapping.Wrap;
                            NameCategory.FontSize = 14; // фиксируем шрифт

                            return;
                        }
                    }

                    NameCategory.Text = categoryId; // fallback
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении Excel: " + ex.Message);
            }
        }

        //Свайпы
        private void SliderContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            swipeStartPoint = e.GetPosition(this);
        }

        private async void SliderContainer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (imageChange) return;
            SliderContainer.IsHitTestVisible = false;
            Point endPoint = e.GetPosition(this);
            double deltaY = endPoint.Y - swipeStartPoint.Y;

            const double swipeThreshold = 20; // порог в пикселях

            if (Math.Abs(deltaY) > swipeThreshold)
            {
                if (deltaY < 0)
                {
                    // Свайп вверх → следующая
                    await NextImageAsync();
                }
                else
                {
                    // Свайп вниз → предыдущая
                    await PrevImageAsync();
                }
            }
            //await Task.Delay(450);
            SliderContainer.IsHitTestVisible = true;
        }
        //Появление фото по одному
        private async Task RevealItemsSequentially()
        {
            int delayMs = 150;
            await Task.Delay(6500);
            foreach (var item in extendedPaths)
            {
                item.Visibility = Visibility.Visible;
                await Task.Delay(delayMs);
            }
        }



        //Загрузка excel
        private void LoadDescriptions(string excelFilePath)
        {
            try
            {
                using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();

                        foreach (DataTable table in result.Tables)
                        {
                            string categoryName = table.TableName.Trim();
                            Dictionary<string, string> descriptions = new Dictionary<string, string>();

                            for (int i = 1; i < table.Rows.Count; i++) // начинаем со 2-й строки
                            {
                                string fileName = table.Rows[i][0]?.ToString()?.Trim() ?? "";
                                string desc = table.Rows[i][1]?.ToString()?.Trim() ?? "";

                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    descriptions[fileName] = desc;
                                }
                            }

                            imageDescriptions[categoryName] = descriptions;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении Excel:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadButton()
        {
            //Загрузка кнопок перелистывания
            //ButtonNext.Position = TimeSpan.Zero;
            //ButtonPrev.Position = TimeSpan.Zero;
            ButtonNext.Play();
            ButtonPrev.Play();

            //Загрузка кнопки описания
            string videoPath = mainFolder + @"\Sprites\Slider_DiscriptionOn.wmv";
            ImageDescButton.Source = new Uri(videoPath);
            ImageDescButton.Opacity = 0;
            ImageDescButton.IsHitTestVisible = false;
            ImageDescButton.Play();
            //Загрузка кнопки описания
            string videoPathRevers = mainFolder + @"\Sprites\Slider_DiscriptionOff.wmv";
            ImageDescButtonRevers.Source = new Uri(videoPathRevers);
            ImageDescButtonRevers.Opacity = 1;
            ImageDescButtonRevers.IsHitTestVisible = true;
            ImageDescButtonRevers.Play();

            //Кнопка зума
            string videoPathZoom = mainFolder + @"\Sprites\Button_zoom.wmv";
            ButtonZoom.Source = new Uri(videoPathZoom);
            ButtonZoom.Play();
            //ButtonZoom.Position = TimeSpan.Zero;

            //Загрузка кнопки слайдшоу
            string videoPathSlide = mainFolder + @"\Sprites\Slider_SlideShowOn.wmv";
            ImageShowButton.Source = new Uri(videoPathSlide);
            ImageShowButton.Opacity = 0;
            ImageShowButton.IsHitTestVisible = false;
            ImageShowButton.Play();
            //Загрузка кнопки слайдшоу
            string videoPathSlideRevers = mainFolder + @"\Sprites\Slider_SlideShowOff.wmv";
            ImageShowButtonRevers.Source = new Uri(videoPathSlideRevers);
            ImageShowButtonRevers.Opacity = 1;
            ImageShowButtonRevers.IsHitTestVisible = true;
            ImageShowButtonRevers.Play();
        }

        // Переключение в полноэкранный режим
        private async void ButtonZoom_Click(object sender, RoutedEventArgs e)
        {
            if (isFullScreen)
            {
                ButtonZoom.IsHitTestVisible = true;
            } 
            else
            {
                ButtonZoom.IsHitTestVisible = false;
            }
            ButtonZoom.Position = TimeSpan.Zero;
            ButtonZoom.Play();
            await Task.Delay(300);
            if (isFullScreen)
            {
                // Выход из полноэкранного режима
                ZoomSlider.Visibility = Visibility.Collapsed;
                ZoomSlider.Value = 1.0; // Сброс масштаба
                ImageDisplay.LayoutTransform = new ScaleTransform(1, 1); // Сброс трансформации
                ImageFullScreen.Visibility = Visibility.Hidden;
                BlackBack.Visibility = Visibility.Hidden;
                CloseZoom.Visibility = Visibility.Hidden;
            }
            else
            {
                // Включение полноэкранного режима
                BlackBack.Visibility = Visibility.Visible;
                ZoomSlider.Visibility = Visibility.Visible;
                ImageFullScreen.Visibility = Visibility.Visible;
                CloseZoom.Visibility = Visibility.Visible;
            }

            isFullScreen = !isFullScreen;
        }

        private void ImageFullScreen_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = MainGrid;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
        }

        private void ImageFullScreen_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Масштаб
            double scaleDelta = e.DeltaManipulation.Scale.X;

            // Применим масштаб
            double newScale = scaleTransform.ScaleX * scaleDelta;

            // Ограничим масштаб
            newScale = Math.Max(1.0, Math.Min(4.0, newScale));

            scaleTransform.ScaleX = newScale;
            scaleTransform.ScaleY = newScale;

            // Смещение (перетаскивание пальцами)
            translateTransform.X += e.DeltaManipulation.Translation.X;
            translateTransform.Y += e.DeltaManipulation.Translation.Y;

            // Не даём "утащить" картинку за экран
            ApplyImageBounds();

            e.Handled = true;
        }


        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double newScale = e.NewValue;
            double oldScale = scaleTransform.ScaleX;

            // Центр масштабирования — относительно текущего положения
            scaleTransform.ScaleX = newScale;
            scaleTransform.ScaleY = newScale;

            // Центрирование при масштабировании
            double deltaScale = newScale - oldScale;
            double deltaX = ImageFullScreen.ActualWidth * deltaScale / 2;
            double deltaY = ImageFullScreen.ActualHeight * deltaScale / 2;

            translateTransform.X -= deltaX;
            translateTransform.Y -= deltaY;

            // Применим ограничение после центрирования
            ApplyImageBounds();
        }

        private void TrackBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var pos = e.GetPosition(border);
            double trackWidth = border.ActualWidth;
            if (trackWidth <= 0) return;

            double relativePosition = pos.X / trackWidth;
            double newValue = ZoomSlider.Minimum + (ZoomSlider.Maximum - ZoomSlider.Minimum) * relativePosition;
            ZoomSlider.Value = newValue;
        }


        private void ZoomSlider_Loaded(object sender, RoutedEventArgs e)
        {
            var slider = sender as Slider;
            if (slider == null) return;

            var trackBackground = FindChild<Border>(slider, "TrackBackground");
            if (trackBackground != null)
            {
                trackBackground.MouseLeftButtonDown += TrackBackground_MouseLeftButtonDown;
            }
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T childType)
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        return childType;
                    }
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }


        private void ImageFullScreen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            start = e.GetPosition(this);
            origin = new Point(translateTransform.X, translateTransform.Y);
            ImageFullScreen.CaptureMouse();
            ImageFullScreen.Cursor = Cursors.Hand;
        }

        private void ImageFullScreen_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ImageFullScreen.ReleaseMouseCapture();
            ImageFullScreen.Cursor = Cursors.Arrow;
        }

        private void ImageFullScreen_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            Vector delta = e.GetPosition(this) - start;
            double newX = origin.X + delta.X;
            double newY = origin.Y + delta.Y;

            translateTransform.X = newX;
            translateTransform.Y = newY;

            ApplyImageBounds();
        }

        private void ApplyImageBounds()
        {
            double scale = scaleTransform.ScaleX;

            double imgWidth = ImageFullScreen.ActualWidth * scale;
            double imgHeight = ImageFullScreen.ActualHeight * scale;

            double containerWidth = MainGrid.ActualWidth;
            double containerHeight = MainGrid.ActualHeight;

            // Максимальное смещение по каждой оси (от центра)
            double maxOffsetX = Math.Max(0, (imgWidth - containerWidth) / 2);
            double maxOffsetY = Math.Max(0, (imgHeight - containerHeight) / 2);

            // Ограничиваем смещения
            translateTransform.X = Math.Max(-maxOffsetX, Math.Min(maxOffsetX, translateTransform.X));
            translateTransform.Y = Math.Max(-maxOffsetY, Math.Min(maxOffsetY, translateTransform.Y));
        }


        //Анимация описания
        private async Task AnimateDescription()
        {
            if (Canvas.GetTop(ImageDescFrame) == 970)
            {
                double startTop = Canvas.GetTop(ImageDescFrame);
                double startHeight = ImageDescFrame.Height;
                double startTopBackground = Canvas.GetTop(ImageDescBackground);
                double startHeightBackgorund = ImageDescBackground.Height;
                double startTopText = Canvas.GetTop(TextBlockDescImage);
                double startHeightText = TextBlockDescImage.Height;

                var tcs = new TaskCompletionSource<bool>();

                //Рамка
                DoubleAnimation DescFrameTop = new DoubleAnimation
                {
                    To = startTop - 195,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescFrameHeight = new DoubleAnimation
                {
                    From = startHeight,
                    To = 220,
                    Duration = TimeSpan.FromSeconds(1.1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DescFrameHeight.Completed += (s, e) => tcs.TrySetResult(true);

                //Фон
                DoubleAnimation DescBackTop = new DoubleAnimation
                {
                    To = startTopBackground - 195,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescBackHeight = new DoubleAnimation
                {
                    From = startHeightBackgorund,
                    To = 190,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                //Текст описания
                DoubleAnimation DescTextTop = new DoubleAnimation
                {
                    To = startTopText - 185,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescTextHeight = new DoubleAnimation
                {
                    From = startHeightText,
                    To = 190,
                    Duration = TimeSpan.FromSeconds(1.1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                ImageDescFrame.BeginAnimation(Canvas.TopProperty, DescFrameTop);
                ImageDescFrame.BeginAnimation(FrameworkElement.HeightProperty, DescFrameHeight);
                ImageDescBackground.BeginAnimation(Canvas.TopProperty, DescBackTop);
                ImageDescBackground.BeginAnimation(FrameworkElement.HeightProperty, DescBackHeight);
                TextBlockDescImage.BeginAnimation(Canvas.TopProperty, DescTextTop);
                TextBlockDescImage.BeginAnimation(FrameworkElement.HeightProperty, DescTextHeight);

                await tcs.Task;
            } 
        }
        private async Task AnimateDescriptionReset()
        {
            if (Canvas.GetTop(ImageDescFrame) == 775)
            {
                double startTop = Canvas.GetTop(ImageDescFrame);
                double startHeight = ImageDescFrame.Height;
                double startTopBackground = Canvas.GetTop(ImageDescBackground);
                double startHeightBackgorund = ImageDescBackground.Height;
                double startTopText = Canvas.GetTop(TextBlockDescImage);
                double startHeightText = TextBlockDescImage.Height;

                var tcs = new TaskCompletionSource<bool>();

                DoubleAnimation DescFrameTop = new DoubleAnimation
                {
                    To = startTop + 195,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescFrameHeight = new DoubleAnimation
                {
                    From = startHeight,
                    To = 17,
                    Duration = TimeSpan.FromSeconds(0.9),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DescFrameHeight.Completed += (s, e) => tcs.TrySetResult(true);

                //Фон
                DoubleAnimation DescBackTop = new DoubleAnimation
                {
                    To = startTopBackground + 195,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescBackHeight = new DoubleAnimation
                {
                    From = startHeightBackgorund,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                //Текст описания
                DoubleAnimation DescTextTop = new DoubleAnimation
                {
                    To = startTopText + 185,
                    Duration = TimeSpan.FromSeconds(1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation DescTextHeight = new DoubleAnimation
                {
                    From = startHeightText,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(1.1),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                ImageDescFrame.BeginAnimation(Canvas.TopProperty, DescFrameTop);
                ImageDescFrame.BeginAnimation(FrameworkElement.HeightProperty, DescFrameHeight);
                ImageDescBackground.BeginAnimation(Canvas.TopProperty, DescBackTop);
                ImageDescBackground.BeginAnimation(FrameworkElement.HeightProperty, DescBackHeight);
                TextBlockDescImage.BeginAnimation(Canvas.TopProperty, DescTextTop);
                TextBlockDescImage.BeginAnimation(FrameworkElement.HeightProperty, DescTextHeight);

                await tcs.Task;
            }
        }

        //ОТкрытие описания
        private async void ImageDescButton_Click(object sender, RoutedEventArgs e)
        {
            ImageDescButton.IsEnabled = false;
            ImageDescButtonRevers.IsEnabled = false;
            ImageDescButtonRevers.Position = TimeSpan.Zero;
            await Task.Delay(20);
            ImageDescButtonRevers.Opacity = 1;
            ImageDescButtonRevers.IsHitTestVisible = true;
            await Task.Delay(30);
            ImageDescButton.Opacity = 0;
            ImageDescButton.IsHitTestVisible = false;
            ImageDescButtonRevers.Play();
            await AnimateDescriptionReset();
            ImageDescButton.IsEnabled = true;
            ImageDescButtonRevers.IsEnabled = true;
        }

        //Закрытие описания
        private async void ImageDescButtonRevers_Click(object sender, RoutedEventArgs e)
        {
            ImageDescButton.IsEnabled = false;
            ImageDescButtonRevers.IsEnabled = false;
            ImageDescButton.Position = TimeSpan.Zero;
            ImageDescButton.Opacity = 1;
            ImageDescButton.IsHitTestVisible = true;
            await Task.Delay(50);
            ImageDescButtonRevers.Opacity = 0;
            ImageDescButtonRevers.IsHitTestVisible = false;
            ImageDescButton.Play();
            await AnimateDescription();
            ImageDescButton.IsEnabled = true;
            ImageDescButtonRevers.IsEnabled = true;
        }

        //Включение слайдшоу
        private async void ImageShowButton_Click(object sender, RoutedEventArgs e)
        {
            ImageShowButton.IsHitTestVisible = false;
            ImageShowButtonRevers.IsHitTestVisible = false;
            ImageShowButtonRevers.Position = TimeSpan.Zero;
            await Task.Delay(20);
            ImageShowButtonRevers.Opacity = 1;
            await Task.Delay(30);
            ImageShowButton.Opacity = 0;
            ImageShowButtonRevers.Play();
            if (isLooping)
            {
                isLooping = false;
                loopTimer.Stop();
                // Можно сменить визуал кнопки или включить анимацию
            }
            await Task.Delay(530);
            ImageShowButtonRevers.IsHitTestVisible = true;
        }

        //Выключение слайдшоу
        private async void ImageShowButtonRevers_Click(object sender, RoutedEventArgs e)
        {
            ImageShowButton.IsHitTestVisible = false;
            ImageShowButtonRevers.IsHitTestVisible = false;
            ImageShowButton.Position = TimeSpan.Zero;
            ImageShowButton.Opacity = 1;
            await Task.Delay(50);
            ImageShowButtonRevers.Opacity = 0;
            ImageShowButton.Play();
            if (!isLooping)
            {
                LoopTimer_Tick(null, null);
                isLooping = true;
                loopTimer.Start();
                // Можно сменить визуал кнопки или включить анимацию
            }
            await Task.Delay(530);
            ImageShowButton.IsHitTestVisible = true;
        }

        //Таймер зацикливания прокрутки
        private async void LoopTimer_Tick(object sender, EventArgs e)
        {
            if (imageChange) return;
            if (scrollingForward)
            {
                if (currentIndex < imagePaths.Count - 1)
                {
                    await NextImageAsync();
                }
                else
                {
                    scrollingForward = false;
                    await PrevImageAsync();
                }
            }
            else
            {
                if (currentIndex > 0)
                {
                    await PrevImageAsync();
                }
                else
                {
                    scrollingForward = true;
                    await NextImageAsync();
                }
            }
        }


        //Закленное проигрывание фона
        private void GearsIdle_Background(string source)
        {
            GearsIdle.MediaEnded += GearsIdle_MediaEnded;
            GearsIdle.LoadedBehavior = MediaState.Manual;
            GearsIdle.Source = new Uri(source);
            GearsIdle.Play();
        }

        private void GearsIdle_MediaEnded(object sender, RoutedEventArgs e)
        {
            GearsIdle.Position = TimeSpan.FromMilliseconds(80);
            GearsIdle.Play();
        }

        private void LoadImages(string categoryPath)
        {
            try
            {
                if (Directory.Exists(categoryPath))
                {
                    // Получаем файлы изображений (PNG, JPG)
                    paths = Directory.GetFiles(categoryPath, "*.png")
                        .Concat(Directory.GetFiles(categoryPath, "*.jpg"))
                        .ToArray();

                    // Очистим списки
                    imagePaths.Clear();
                    extendedPaths.Clear();

                    // Заполним основной список imagePaths
                    foreach (var path in Directory.GetFiles(categoryPath, "*.*")
                                 .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png")))
                    {
                        imagePaths.Add(path);
                    }

                    // Добавляем клоны: последние N элементов в начало
                    for (int i = imagePaths.Count - MaxVisibleItems; i < imagePaths.Count; i++)
                    {
                        if (i >= 0)
                            extendedPaths.Add(new ImageItem { Path = imagePaths[i], Visibility = Visibility.Collapsed });
                    }

                    // Основной список
                    foreach (var path in imagePaths)
                    {
                        extendedPaths.Add(new ImageItem { Path = path, Visibility = Visibility.Collapsed });
                    }

                    // Первые N в конец
                    for (int i = 0; i < MaxVisibleItems; i++)
                    {
                        extendedPaths.Add(new ImageItem { Path = imagePaths[i], Visibility = Visibility.Collapsed });
                    }

                    // Устанавливаем стартовую позицию
                    scrollOffset = MaxVisibleItems * ImageHeight;
                    scrollViewer.ScrollToVerticalOffset(scrollOffset);

                    if (paths.Length > 0)
                    {
                        ShowImage(3);
                    }
                    else
                    {
                        MessageBox.Show("В этой категории нет изображений.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Указанный путь категории не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("Ошибка: В категории недостаточно фотографий.",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (player != null)
                {
                    _ = player.CloseWithFadeOut();
                }

                this.Close();
            }
        }

        //Фото по центру
        private void ShowImage(int index)
        {
            if (imageDescriptions.ContainsKey(currentCategory))
            {
                var dict = imageDescriptions[currentCategory];
                string filename = System.IO.Path.GetFileNameWithoutExtension(imagePaths[index]);

                if (dict.ContainsKey(filename))
                {
                    TextBlockDescImage.Text = dict[filename];
                }
                else
                {
                    TextBlockDescImage.Text = "Нет описания"; // Нет описания
                }
            }
            else
            {
                TextBlockDescImage.Text = "Нет категории"; // Нет такой категории
            }

            if (imagePaths == null || imagePaths.Count == 0) return;
            if (index < 0 || index >= imagePaths.Count) return;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePaths[index], UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // улучшает производительность и сборку мусора


            // Анимация плавного появления
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            ImageDisplay.Source = bitmap;
            ImageFullScreen.Source = bitmap;
            ImageDisplay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private async void StartImageDisplayAnim()
        {
            await Task.Delay(8500);
            // Сбросим прозрачность и запустим анимацию
            

            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(3000),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            StartImageDisplay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private async void NextImage_Click(object sender, RoutedEventArgs e)
        {
            if (imageChange) return;
            ButtonPrev.IsHitTestVisible = false;
            ButtonNext.IsHitTestVisible = false;
            ButtonNext.Position = TimeSpan.Zero;
            ButtonNext.Play();
            await NextImageAsync();
        }

        private async Task NextImageAsync()
        {
            if (imagePaths == null || paths.Length == 0) return;
            imageChange = true;
            var tcs = new TaskCompletionSource<bool>();
            if (currentIndex < paths.Length - 1)
            {
                currentIndex++;
                ShowImage(currentIndex);

                if (isAnimating) return;
                isAnimating = true;

                MoveNthItemRight(indexFirstFoto);
                indexFirstFoto++;

                ResetSkewAndMoveEffect(indexSecondFoto);
                indexSecondFoto++;
                ResetSkewAndMoveEffectNext(indexSecondFoto);

                scrollOffset += ImageHeight;

                // Округление после увеличения scrollOffset
                scrollOffset = Math.Round(scrollOffset / ImageHeight) * ImageHeight; // Округляем до кратного ImageHeight


                AnimateScroll(scrollViewer, scrollOffset, () =>
                {
                    if (scrollOffset >= (imagePaths.Count + MaxVisibleItems) * ImageHeight)
                    {
                        scrollOffset = MaxVisibleItems * ImageHeight;
                        scrollViewer.ScrollToVerticalOffset(scrollOffset);
                    }
                    isAnimating = false;
                    tcs.TrySetResult(true);
                });

                await tcs.Task;
                await Task.Delay(500);
                ButtonNext.IsHitTestVisible = true;
            }
            else
            {
                ButtonNext.IsHitTestVisible = false;
            }
            ButtonPrev.IsHitTestVisible = true;
            imageChange = false;
        }

        //Метод искажения первого элемента
        private void MoveNthItemRight(int index)
        {
            if (!itemElements.ContainsKey(index))
            {
                return;
            }

            FrameworkElement targetItem = itemElements[index];

            // Проверяем, есть ли уже TransformGroup, если нет – создаем
            var transformGroup = targetItem.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                var skew = new SkewTransform();
                var translate = new TranslateTransform();
                transformGroup.Children.Add(skew);
                transformGroup.Children.Add(translate);
                targetItem.RenderTransform = transformGroup;
            }

            var skewTransform = transformGroup.Children[0] as SkewTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            // Анимация искажения (наклон в трапецию)
            DoubleAnimation skewAnimation = new DoubleAnimation
            {
                To = 12, // Угол наклона вправо (трапеция)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Анимация смещения влево
            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                To = -18, // Смещение влево (отрицательное значение)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            skewTransform.BeginAnimation(SkewTransform.AngleXProperty, skewAnimation);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
        }

        //Метод возврата к исходному положению элемента
        private void ResetSkewAndMoveEffect(int index)
        {
            if (!itemElements.ContainsKey(index))
            {
                return;
            }

            FrameworkElement targetItem = itemElements[index];

            // Проверяем, есть ли уже TransformGroup, если нет – создаем
            var transformGroup = targetItem.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                var skew = new SkewTransform();
                var translate = new TranslateTransform();
                transformGroup.Children.Add(skew);
                transformGroup.Children.Add(translate);
                targetItem.RenderTransform = transformGroup;
            }

            var skewTransform = transformGroup.Children[0] as SkewTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            // Анимация искажения (наклон в трапецию)
            DoubleAnimation skewAnimation = new DoubleAnimation
            {
                To = 0, // Угол наклона вправо (трапеция)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Анимация смещения влево
            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                To = 0, // Смещение влево (отрицательное значение)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            skewTransform.BeginAnimation(SkewTransform.AngleXProperty, skewAnimation);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
        }

        //Поя
        private void ResetSkewAndMoveEffectNext(int index)
        {
            if (!itemElements.ContainsKey(index))
            {
                return;
            }

            FrameworkElement targetItem = itemElements[index];

            // Проверяем, есть ли уже TransformGroup, если нет – создаем
            var transformGroup = targetItem.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                var skew = new SkewTransform();
                var translate = new TranslateTransform(); // только смещение по X, не меняем размеры
                transformGroup.Children.Add(skew);
                transformGroup.Children.Add(translate);
                targetItem.RenderTransform = transformGroup;
            }

            var skewTransform = transformGroup.Children[0] as SkewTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            // Используем только смещение по оси X, без изменения высоты
            DoubleAnimation skewAnimation = new DoubleAnimation
            {
                To = -10, // Угол наклона вправо (трапеция)
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                To = 0, // Смещение влево
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            skewTransform.BeginAnimation(SkewTransform.AngleXProperty, skewAnimation);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation); // Меняем только положение по X, не высоту
        }

        private Dictionary<int, FrameworkElement> itemElements = new Dictionary<int, FrameworkElement>(); // Храним элементы по индексу

        private void OnItemLoaded(object sender, RoutedEventArgs e)
        {
            int index = itemElements.Count; // Текущий индекс (по порядку загрузки)
            itemElements[index] = sender as FrameworkElement;

            Console.WriteLine($"Загружен элемент с индексом {index}");

            // Если уже найден 1-й элемент, не продолжаем
            if (index == indexFirstFoto - 1)
            {
                Console.WriteLine("Найден 10-й элемент! Готов к сдвигу.");
                ApplySkewAndMoveEffect(itemElements[index]);
            }

            // Если уже найден последний элемент, не продолжаем
            if (index == indexSecondFoto)
            {
                Console.WriteLine("Найден 10-й элемент! Готов к сдвигу.");
                ApplySkewAndMoveEffectSecond(itemElements[index]);
            }

        }

        //Сдвиг первого элемента при загрузке
        private void ApplySkewAndMoveEffect(FrameworkElement targetItem)
        {
            if (targetItem == null) return;

            // Создаем TransformGroup, если его еще нет
            var transformGroup = new TransformGroup();
            var skew = new SkewTransform { AngleX = 10 }; // Угол наклона (трапеция)
            var translate = new TranslateTransform { X = -15 }; // Смещение влево
            transformGroup.Children.Add(skew);
            transformGroup.Children.Add(translate);

            // Применяем трансформации к элементу
            targetItem.RenderTransform = transformGroup;

            Console.WriteLine("Искажение и сдвиг применены к 10-му элементу.");
        }

        //Сдвиг последнего элемента при загрузке
        private void ApplySkewAndMoveEffectSecond(FrameworkElement targetItem)
        {
            if (targetItem == null) return;

            // Создаем TransformGroup, если его еще нет
            var transformGroup = new TransformGroup();
            var skew = new SkewTransform { AngleX = -10 }; // Угол наклона (трапеция)
            var translate = new TranslateTransform { X = 0 }; // Смещение влево
            transformGroup.Children.Add(skew);
            transformGroup.Children.Add(translate);

            // Применяем трансформации к элементу
            targetItem.RenderTransform = transformGroup;

            Console.WriteLine("Искажение и сдвиг применены к 10-му элементу.");
        }

        private async void PrevImage_Click(object sender, RoutedEventArgs e)
        {
            if (imageChange) return;
            ButtonNext.IsHitTestVisible = false;
            ButtonPrev.IsHitTestVisible = false;
            ButtonPrev.Position = TimeSpan.Zero;
            ButtonPrev.Play();
            await PrevImageAsync();
        }

        private async Task PrevImageAsync()
        {
            if (paths == null || paths.Length == 0) return;
            imageChange = true;
            var tcs = new TaskCompletionSource<bool>();
            if (currentIndex > 0)
            {
                currentIndex--;
                ShowImage(currentIndex);

                if (isAnimating) return;
                isAnimating = true;

                if (indexFirstFoto > 0) // Проверяем, чтобы не выйти за границы
                {
                    indexSecondFoto--;
                    MoveNthItemLeft(indexSecondFoto);

                    indexFirstFoto--;
                    ApplySkewAndMoveEffect(itemElements[indexFirstFoto - 1]);
                    ResetSkewAndMoveEffect(indexFirstFoto); // Возвращаем эффект для следующего в очереди
                }

                scrollOffset -= ImageHeight;

                // Округление после уменьшения scrollOffset
                scrollOffset = Math.Round(scrollOffset / ImageHeight) * ImageHeight; // Округляем до кратного ImageHeight


                AnimateScroll(scrollViewer, scrollOffset, () =>
                {
                    if (scrollOffset <= 0)
                    {
                        scrollOffset = (imagePaths.Count) * ImageHeight;
                        scrollViewer.ScrollToVerticalOffset(scrollOffset);
                    }
                    isAnimating = false;
                    tcs.TrySetResult(true);
                });
                await tcs.Task;
                await Task.Delay(500);

                ButtonPrev.IsHitTestVisible = true;
            }
            else
            {
                ButtonPrev.IsHitTestVisible = false;
            }
            ButtonNext.IsHitTestVisible = true;
            imageChange = false;
        }

        private void MoveNthItemLeft(int index)
        {
            if (!itemElements.ContainsKey(index))
            {
                return;
            }

            FrameworkElement targetItem = itemElements[index];

            // Проверяем, есть ли уже TransformGroup, если нет – создаем
            var transformGroup = targetItem.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                var skew = new SkewTransform();
                var translate = new TranslateTransform();
                transformGroup.Children.Add(skew);
                transformGroup.Children.Add(translate);
                targetItem.RenderTransform = transformGroup;
            }

            var skewTransform = transformGroup.Children[0] as SkewTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            // Обратная анимация искажения (наклон влево)
            DoubleAnimation skewAnimation = new DoubleAnimation
            {
                To = -14, // Угол наклона влево (трапеция)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Обратная анимация смещения вправо
            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                To = 0, // Смещение вправо (положительное значение)
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            skewTransform.BeginAnimation(SkewTransform.AngleXProperty, skewAnimation);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
        }

        private void ResetSkewAndMoveEffectPrev(int index)
        {
            if (!itemElements.ContainsKey(index))
            {
                return;
            }

            FrameworkElement targetItem = itemElements[index];

            var transformGroup = targetItem.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                return;
            }

            var skewTransform = transformGroup.Children[0] as SkewTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            // Сброс анимации искажения (восстанавливаем прямоугольную форму)
            DoubleAnimation skewResetAnimation = new DoubleAnimation
            {
                To = -10,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Сброс смещения в исходное положение
            DoubleAnimation moveResetAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            skewTransform.BeginAnimation(SkewTransform.AngleXProperty, skewResetAnimation);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveResetAnimation);
        }

        private void AnimateScroll(ScrollViewer target, double toValue, Action onCompleted)
        {
            // Округляем до кратного ImageHeight
            toValue = Math.Round(toValue / ImageHeight) * ImageHeight;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            animation.Completed += (s, e) =>
            {
                // Применяем ScrollToVerticalOffset только после завершения анимации
                target.ScrollToVerticalOffset(toValue);
                onCompleted();
            };

            target.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
        }

        private async void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            string videoTrans = mainFolder + @"\Sprites\TranslationOff.wmv";
            var player = new VideoPlayerWindow(videoTrans);
            player.MediaEnded += async (s, args) =>
            {
                ButtonNext.Stop();
                ButtonNext.Source = null;
                ButtonNext.Close(); // Для некоторых версий WPF
                ButtonPrev.Stop();
                ButtonPrev.Source = null;
                ButtonPrev.Close(); // Для некоторых версий WPF
                GearsIdle.Stop();
                GearsIdle.Source = null;
                GearsIdle.Close(); // Для некоторых версий WPF
                this.Content = null;
                this.Close();
                await player.CloseWithFadeOut();
            };
            DoubleAnimation fadeIn = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            fadeIn.Completed += (s, a) => player.Show();
            fadeIn.Completed += (s, a) => inactivityTimer.Stop();
            ImageDisplay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            
        }
    }
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer viewer)
            {
                viewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
    public class ImageItem : INotifyPropertyChanged
    {
        private Visibility _visibility = Visibility.Collapsed;

        public string Path { get; set; }

        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
