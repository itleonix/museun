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
using System.IO;
using System.Windows.Media.Animation;
using System.Reflection.Emit;
using OfficeOpenXml;
using System.Diagnostics;
using System.Windows.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Numerics;
using System.Configuration;
using System.Windows.Interop;

namespace Museum3
{
    /// <summary>
    /// Логика взаимодействия для ContainerImage.xaml
    /// </summary>
    public partial class ContainerImage : UserControl
    {
        protected string mainFolder = ConfigurationManager.AppSettings["MainFolder"];
        private string excelPath;
        private bool isCropped = false;
        public string CategoryName { get; set; }

        private GalleryWindow galleryWindow; // Поле для хранения ссылки на окно
        private WeakReference<GalleryWindow> galleryWindowRef;
        private double? originalTop = null;
        private int positionOnScreen; // 0–7
        private Point buttonDescSwipeStart;
        private const double SwipeThreshold = 30;
        private bool isOpenButtonEnabled = true;


        public readonly string[] VideoPaths;

        public ContainerImage(int position)
        {
            excelPath = mainFolder + @"\catalog\descCategory.xlsx";
            VideoPaths = new string[]
            {
                mainFolder + @"\Sprites\Choose1.wmv",
                mainFolder + @"\Sprites\Choose2.wmv",
                mainFolder + @"\Sprites\Choose3.wmv",
                mainFolder + @"\Sprites\Choose4.wmv",
                mainFolder + @"\Sprites\Choose5.wmv",
                mainFolder + @"\Sprites\Choose6.wmv",
                mainFolder + @"\Sprites\Choose7.wmv",
                mainFolder + @"\Sprites\Choose8.wmv",
            };
            InitializeComponent();
            //ButtonDesc.MouseLeftButtonDown += ButtonDesc_Click;
            positionOnScreen = position;
            TextBlockDesc.FontSize = int.Parse(ConfigurationManager.AppSettings["MainDescFontSize"]);
            CategoryLabel.FontSize = int.Parse(ConfigurationManager.AppSettings["MainTitleFontSize"]);
        }

        //Центрируем label
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CenterLabel(); // Центрируем после загрузки окна
        }

        private void CategotyLabel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterLabel(); // Перецентрируем, когда Label получил свои размеры
        }

        //Картинка для категории и название категории
        public void SetCategory(string categoryPath)
        {
            CategoryName = System.IO.Path.GetFileName(categoryPath); //Название категории
            LoadCategoryData(CategoryName);

            string[] images = Directory.GetFiles(categoryPath, "*.jpg");
            if (images.Length > 0)
            {
                ContainerFoto.Source = new BitmapImage(new Uri(images[0], UriKind.Absolute));
            }
        }

        // Возвращаем только нужный CanvasDesc
        public Canvas GetCanvasDesc()
        {
            return this.FindName("CanvasDesc") as Canvas;
        }

        //Функция загрузка имени и описания категорий
        private void LoadCategoryData(string categoryName)
        {
            int maxLength = 200; //КОличество отображаемых символов

            if (!File.Exists(excelPath))
            {
                MessageBox.Show("Файл descCategory.xlsx не найден!");
                return;
            }

            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(excelPath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Первый лист

                    int rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++) // Пропускаем заголовки
                    {
                        string id = worksheet.Cells[row, 1].Text; // Столбец A (ID)
                        string description = worksheet.Cells[row, 2].Text; // Столбец B (Description)
                        string name = worksheet.Cells[row, 3].Text; // Столбец C (Name)

                        if (id.Equals(categoryName, StringComparison.OrdinalIgnoreCase)) // Поиск по ID
                        {
                            CategoryLabel.Content = name; // Устанавливаем название категории
                            TextBlockDesc.Text = description.Length > maxLength ? description.Substring(0, maxLength) + "..." : description; ; // Устанавливаем описание

                            return;
                        }
                    }

                    MessageBox.Show($"Категория '{categoryName}' не найдена в Excel.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке Excel: {ex.Message}");
            }
        }

        //Возвращаем на место все кнопки и плашки
        public async Task AnimateResetDescState()
        {
            var duration = new Duration(TimeSpan.FromSeconds(0.7));
            IEasingFunction easing = new QuarticEase { EasingMode = EasingMode.EaseOut };

            var animationTasks = new List<Task>();

            void AnimateIfChanged(DependencyObject target, DependencyProperty property, double current, double targetValue)
            {
                if (Math.Abs(current - targetValue) > 0.1)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    var anim = new DoubleAnimation
                    {
                        To = targetValue,
                        Duration = duration,
                        EasingFunction = easing
                    };
                    anim.Completed += (s, e) => tcs.SetResult(true);
                    (target as UIElement)?.BeginAnimation(property, anim);
                    animationTasks.Add(tcs.Task);
                }
                else
                {
                    (target as UIElement)?.BeginAnimation(property, null);
                    if (property == Canvas.LeftProperty)
                        Canvas.SetLeft((UIElement)target, targetValue);
                    else if (property == Canvas.TopProperty)
                        Canvas.SetTop((UIElement)target, targetValue);
                    else if (property == FrameworkElement.HeightProperty)
                        ((FrameworkElement)target).Height = targetValue;
                }
            }

            // --- ButtonDesc ---
            double buttonLeft = Canvas.GetLeft(ButtonDesc);
            AnimateIfChanged(ButtonDesc, Canvas.LeftProperty, buttonLeft, 68);
            ButtonDesc.Clip = null; // Убираем обрезку

            //ButtonDescLight
            double buttonLeftLight = Canvas.GetLeft(ButtonDescLight);
            AnimateIfChanged(ButtonDescLight, Canvas.LeftProperty, buttonLeftLight, 79.5);
            AnimateIfChanged(ButtonDescLight, UIElement.OpacityProperty, 1, 0);

            // --- Chain ---
            double chainLeft = Canvas.GetLeft(Chain);
            AnimateIfChanged(Chain, Canvas.LeftProperty, chainLeft, 11);

            // --- Frame ---
            double frameTop = Canvas.GetTop(Frame);
            AnimateIfChanged(Frame, Canvas.TopProperty, frameTop, 100);

            // --- ContainerFoto ---
            double fotoTop = Canvas.GetTop(ContainerFoto);
            AnimateIfChanged(ContainerFoto, Canvas.TopProperty, fotoTop, 110);

            // --- CanvasDesc: Top + Height одновременно ---
            double canvasTop = Canvas.GetTop(CanvasDesc);
            double canvasHeight = CanvasDesc.ActualHeight;
            if (Math.Abs(canvasTop - 110) > 0.1 || Math.Abs(canvasHeight - 1) > 0.1)
            {
                var tcs = new TaskCompletionSource<bool>();
                int completeCount = 0;

                var animTop = new DoubleAnimation
                {
                    To = 110,
                    Duration = duration,
                    EasingFunction = easing
                };
                var animHeight = new DoubleAnimation
                {
                    From = canvasHeight,
                    To = 1,
                    Duration = duration,
                    EasingFunction = easing
                };

                animTop.Completed += (s, e) => { if (++completeCount == 2) tcs.SetResult(true); };
                animHeight.Completed += (s, e) => { if (++completeCount == 2) tcs.SetResult(true); };

                CanvasDesc.BeginAnimation(Canvas.TopProperty, animTop);
                CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, animHeight);
                animationTasks.Add(tcs.Task);
            }
            else
            {
                CanvasDesc.BeginAnimation(Canvas.TopProperty, null);
                CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, null);
                Canvas.SetTop(CanvasDesc, 110);
                CanvasDesc.Height = 1;
            }

            // --- TextBlockDesc (или TextBox) ---
            double textHeight = TextBlockDesc.ActualHeight;
            if (Math.Abs(textHeight - 1) > 0.1)
            {
                var tcsText = new TaskCompletionSource<bool>();
                var animTextHeight = new DoubleAnimation
                {
                    From = textHeight,
                    To = 1,
                    Duration = duration,
                    EasingFunction = easing
                };
                animTextHeight.Completed += (s, e) => tcsText.SetResult(true);
                TextBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, animTextHeight);
                animationTasks.Add(tcsText.Task);
            }
            else
            {
                TextBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, null);
                TextBlockDesc.Height = 1;
            }

            // Ожидаем завершения всех анимаций
            await Task.WhenAll(animationTasks);

            // Финальный сброс состояния
            isCropped = false;
        }

        //Свайп по кнопке описания
        private void ButtonDesc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            buttonDescSwipeStart = e.GetPosition(ButtonDesc);
        }

        private void ButtonDesc_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point endPoint = e.GetPosition(ButtonDesc);
            double deltaX = endPoint.X - buttonDescSwipeStart.X;

            if (Math.Abs(deltaX) > SwipeThreshold)
            {
                // Свайп вправо или влево — вызываем "нажатие" вручную
                ButtonDesc_Click(ButtonDesc, e);
            }
        }

        // Обработчик клика по ButtonDesc
        private async void ButtonDesc_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MessageBox.Show($"Категория: {CategoryName}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

            //Анимация движения кнопки описания
            double newLeft = 0;
            double newLeftLight = 0;

            Image buttonDesc = sender as Image; //Получаем нажатый объект
            if (buttonDesc == null) return;

            if (!isCropped)
            {
                // ⛔️ Отключаем анимацию, иначе SetTop не работает
                CanvasDesc.BeginAnimation(Canvas.TopProperty, null);
                CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, null);
                TextBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, null);
                Canvas.SetTop(CanvasDesc, 90);
            }

            //Задаем диапазон смещения кнопки
            double startPos = Canvas.GetLeft(buttonDesc);
            
            if (startPos == 68)
            {
                newLeft = Canvas.GetLeft(buttonDesc) + 85;
                newLeftLight = Canvas.GetLeft(ButtonDescLight) + 85;
            } else
            {
                newLeft = Canvas.GetLeft(buttonDesc) - 85;
                newLeftLight = Canvas.GetLeft(ButtonDescLight) - 85;
            }

            //Анимация движения кнопки
            DoubleAnimation moveAnimation = new DoubleAnimation
            {
                To = newLeft,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Движение Лампочки
            DoubleAnimation moveAnimationLight = new DoubleAnimation
            {
                To = newLeftLight,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Включение лампочки
            var fadeInLight = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            //Выключение лампочки
            var fadeOutLight = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            //Анимация движения трака
            double startPosChain = Canvas.GetLeft(Chain);
            double newLeftChain = 0;

            if (startPosChain == 11)
            {
                newLeftChain = Canvas.GetLeft(Chain) + 43;
            } else
            {
                newLeftChain = Canvas.GetLeft(Chain) - 43;
            }

            DoubleAnimation moveAnimationChain = new DoubleAnimation
            {
                To = newLeftChain,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Анимация рамки для фото
            double startPosFrame = Canvas.GetTop(Frame);
            double newTopFrame = 0;

            if (startPosFrame == 100)
            {
                newTopFrame = Canvas.GetTop(Frame) - 25;
            }
            else
            {
                newTopFrame = Canvas.GetTop(Frame) + 25;
            }

            DoubleAnimation moveAnimationFrame = new DoubleAnimation
            {
                To = newTopFrame,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Анимация фото
            double startPosFoto = Canvas.GetTop(ContainerFoto);
            double newTopFoto = 0;

            if (startPosFoto == 110)
            {
                newTopFoto = Canvas.GetTop(ContainerFoto) - 25;
            }
            else
            {
                newTopFoto = Canvas.GetTop(ContainerFoto) + 25;
            }

            DoubleAnimation moveAnimationFoto = new DoubleAnimation
            {
                To = newTopFoto,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };

            //Выключаем кликабельность пока работает анимация
            buttonDesc.IsHitTestVisible = false;
            moveAnimationFoto.Completed += (s, a) => buttonDesc.IsHitTestVisible = true;
            if (startPos == 68)
            {
                ButtonDescLight.BeginAnimation(UIElement.OpacityProperty, fadeInLight);
            } else
            {
                ButtonDescLight.BeginAnimation(UIElement.OpacityProperty, fadeOutLight);
            }
            

            //Запускаем анимации
            buttonDesc.BeginAnimation(Canvas.LeftProperty, moveAnimation);
            ButtonDescLight.BeginAnimation(Canvas.LeftProperty, moveAnimationLight);
            Chain.BeginAnimation(Canvas.LeftProperty, moveAnimationChain);
            
            //Задержка запуска анимации сокрытия штыря
            if (isCropped)
            {
                //await Task.Delay(50);
                ExpandImage(buttonDesc);
                ExpandDesc(CanvasDesc);
                await Task.Delay(250);
                Frame.BeginAnimation(Canvas.TopProperty, moveAnimationFrame);
                ContainerFoto.BeginAnimation(Canvas.TopProperty, moveAnimationFoto);
            } else
            {
                Frame.BeginAnimation(Canvas.TopProperty, moveAnimationFrame);
                ContainerFoto.BeginAnimation(Canvas.TopProperty, moveAnimationFoto);
                await Task.Delay(100);
                StartCropAnimationDesc(CanvasDesc, TextBlockDesc);
                await Task.Delay(120);
                StartCropAnimation(buttonDesc);
                
            }

            isCropped = !isCropped; // Меняем флаг
            
        }

        //Функции обрезки кнопки
        private void StartCropAnimation(Image buttonDesc)
        {
            double newWidth = buttonDesc.Width - 12;

            RectangleGeometry clipGeametry = new RectangleGeometry(new Rect(0, 0, buttonDesc.Width, buttonDesc.Height));
            buttonDesc.Clip = clipGeametry;

            RectAnimation cropAnimation = new RectAnimation
            {
                From = new Rect(0, 0, buttonDesc.Width, buttonDesc.Height),
                To = new Rect(0, 0, newWidth, buttonDesc.Height),
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            clipGeametry.BeginAnimation(RectangleGeometry.RectProperty, cropAnimation);
        }

        private void ExpandImage(Image buttonDesc)
        {
            double newWidth = buttonDesc.Width - 12;

            RectangleGeometry clipGeametry = new RectangleGeometry(new Rect(0, 0, newWidth, buttonDesc.Height));
            buttonDesc.Clip = clipGeametry;

            RectAnimation cropAnimation = new RectAnimation
            {
                From = new Rect(0, 0, newWidth, buttonDesc.Height),
                To = new Rect(0, 0, buttonDesc.Width, buttonDesc.Height),
                Duration = TimeSpan.FromSeconds(0.05),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };

            clipGeametry.BeginAnimation(RectangleGeometry.RectProperty, cropAnimation);
        }

        //Функции обрезки блока описания
        private void StartCropAnimationDesc(Canvas canvasDesc, TextBox textBlockDesc)
        {
            Canvas.SetTop(CanvasDesc, 90);

            double newHeight = canvasDesc.Height + 145; // Новая высота

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = canvasDesc.ActualHeight, // Текущая высота
                To = newHeight, // Конечная высота
                Duration = TimeSpan.FromSeconds(0.3), // Время анимации
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } // Плавность
            };

            DoubleAnimation heightAnimationText = new DoubleAnimation
            {
                From = textBlockDesc.ActualHeight, // Текущая высота
                To = newHeight, // Конечная высота
                Duration = TimeSpan.FromSeconds(0.3), // Время анимации
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } // Плавность
            };

            canvasDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
            textBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimationText);
        }

        public void ExpandDesc(Canvas canvasDesc)
        {
            double newHeight = canvasDesc.Height - 145; // Новая высота
            double newTopCanvasDesc = Canvas.GetTop(CanvasDesc); //Новое размещение

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = canvasDesc.ActualHeight, // Текущая высота
                To = newHeight, // Конечная высота
                Duration = TimeSpan.FromSeconds(0.4), // Время анимации
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } // Плавность
            };
            DoubleAnimation moveAnimationTopDesc = new DoubleAnimation
            {
                To = newTopCanvasDesc,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut, }
            };
            DoubleAnimation heightAnimationText = new DoubleAnimation
            {
                From = TextBlockDesc.ActualHeight, // Текущая высота
                To = newHeight, // Конечная высота
                Duration = TimeSpan.FromSeconds(0.3), // Время анимации
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } // Плавность
            };

            canvasDesc.BeginAnimation(Canvas.TopProperty, moveAnimationTopDesc);
            canvasDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
            TextBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimationText);
            originalTop = Canvas.GetTop(CanvasDesc);
        }

        //Для общего опускания плашки
        public async Task PartialExpandDesc()
        {
            CanvasDesc.BeginAnimation(Canvas.TopProperty, null);
            CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, null);
            TextBlockDesc.BeginAnimation(FrameworkElement.HeightProperty, null);

            await AnimateResetDescState();

            if (originalTop == null || originalTop == 90)
            {
                originalTop = Canvas.GetTop(CanvasDesc);
            }

            double newTop = originalTop.Value + 20;
            Canvas.SetTop(CanvasDesc, newTop);

            Debug.WriteLine($"Top установлен сразу: {newTop}");

            // Если высота 0, устанавливаем минимальное значение перед анимацией
            if (CanvasDesc.ActualHeight == 0)
            {
                CanvasDesc.Height = 0; // WPF не анимирует элементы с 0 высотой
                CanvasDesc.UpdateLayout();
                await Task.Delay(50); // Даем время на обновление
            }

            double currentHeight = CanvasDesc.ActualHeight;
            double newHeight = currentHeight + 145; // Делаем шторку видимой

            // Сбрасываем предыдущие анимации, если были
            CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, null);

            var tcs = new TaskCompletionSource<bool>();

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = currentHeight,
                To = newHeight,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            heightAnimation.Completed += (s, e) =>
            {
                Debug.WriteLine($"Анимация завершена: Height {newHeight}");
                tcs.TrySetResult(true);
            };

            CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

            await tcs.Task;

            Dispatcher.Invoke(() =>
            {
                CanvasDesc.UpdateLayout();
                Debug.WriteLine("Макет обновлён после анимации.");
            });
        }

        public async Task PartialExpandDescReset()
        {
            // Устанавливаем новое положение перед началом анимации
            double newTop = Canvas.GetTop(CanvasDesc);
            //Canvas.SetTop(CanvasDesc, newTop);
            if (originalTop != null)
            {
                Canvas.SetTop(CanvasDesc, originalTop.Value + 20);
            }

            // Если высота 0, устанавливаем минимальное значение перед анимацией
            if (CanvasDesc.ActualHeight == 0)
            {
                CanvasDesc.Height = 0; // WPF не анимирует элементы с 0 высотой
                CanvasDesc.UpdateLayout();
                await Task.Delay(50); // Даем время на обновление
            }

            double currentHeight = CanvasDesc.ActualHeight;
            double newHeight = currentHeight - 145; // Делаем шторку видимой

            // Сбрасываем предыдущие анимации, если были
            CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, null);

            var tcs = new TaskCompletionSource<bool>();

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = currentHeight,
                To = newHeight,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            heightAnimation.Completed += (s, e) =>
            {
                Debug.WriteLine($"Анимация завершена: Height {newHeight}");
                tcs.TrySetResult(true);
            };

            CanvasDesc.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

            await tcs.Task;

            Dispatcher.Invoke(() =>
            {
                CanvasDesc.UpdateLayout();
                Debug.WriteLine("Макет обновлён после анимации.");
            });
        }

        //Центрирование label
        private void CenterLabel()
        {
            if (CanvasNameCat.ActualWidth == 0 || CanvasNameCat.ActualHeight == 0)
                return; // Ждём, пока Canvas получит размеры

            double labelWidth = CategoryLabel.ActualWidth;
            double labelHeight = CategoryLabel.ActualHeight;

            double canvasWidth = CanvasNameCat.ActualWidth;
            double canvasHeight = CanvasNameCat.ActualHeight;

            // Вычисляем центр
            double left = (canvasWidth - labelWidth) / 2;
            double top = (canvasHeight - labelHeight) / 2;

            // Устанавливаем координаты
            CategoryLabel.SetValue(Canvas.LeftProperty, left);
            //MyLabel.SetValue(Canvas.TopProperty, top);
        }

        public void SetOpenButtonEnabled(bool isEnabled)
        {
            isOpenButtonEnabled = isEnabled;

            if (ButtonOpen != null)
            {
                ButtonOpen.IsHitTestVisible = isEnabled;
            }
        }


        private async void ButtonOpen_Click(object sender, MouseButtonEventArgs e)
        {

            try
            {
                if (!isOpenButtonEnabled)
                    return;

                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.SetAllOpenButtonsEnabled(false);
                mainWindow?.ButtonDisable();
                if (!string.IsNullOrEmpty(CategoryName))
                {
                    string categoryPath = System.IO.Path.Combine(mainWindow?.RootPath, CategoryName); // Формируем полный путь к категории

                    if (Directory.Exists(categoryPath))
                    {
                        //galleryWindow.Show();

                        //Dispatcher.InvokeAsync(() => galleryWindow.Activate(), DispatcherPriority.ApplicationIdle);

                        if (positionOnScreen >= 0 && positionOnScreen < VideoPaths.Length)
                        {
                            string videoPath = VideoPaths[positionOnScreen];
                            if (File.Exists(videoPath))
                            {
                                mainWindow?.AnimatePlank();
                                await mainWindow?.PartialExpandAllDescriptions();
                                //await Task.Delay(300);
                                await mainWindow?.LoadAlbumUp(videoPath);
                                await Task.Delay(3000);
                                string videoTrans = mainFolder + @"\Sprites\Translation.wmv";
                                if (File.Exists(videoTrans))
                                {
                                    var player = new VideoPlayerWindow(videoTrans);
                                    galleryWindow = new GalleryWindow(categoryPath, player); // Передаём ПОЛНЫЙ путь
                                    galleryWindowRef = new WeakReference<GalleryWindow>(galleryWindow);
                                    galleryWindow.Closed += GalleryWindow_Closed;
                                    galleryWindow.Visibility = Visibility.Hidden;
                                    player.MediaEnded += async (s, args) =>
                                    {
                                        galleryWindow.Visibility = Visibility.Visible;
                                        galleryWindow.Activate();
                                        galleryWindow.Show();
                                        //await Task.Delay(100);
                                        await player.CloseWithFadeOut();
                                    };
                                    player.Show(); // Воспроизведение на весь экран
                                    //await Task.Delay(2800);
                                }
                                else
                                {
                                    MessageBox.Show("Видео не найдено.");
                                }
                                await Task.Delay(1000);
                                mainWindow?.AnimatePlankReset();
                                mainWindow?.PartialExpandAllDescriptionsReset();
                                mainWindow?.SetAllOpenButtonsEnabled(true);
                                mainWindow?.ButtonEnsable();
                            }
                            else
                            {
                                MessageBox.Show("Видео не найдено.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Категория не найдена в каталоге.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Имя категории не задано.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("Ошибка: Критичное закрытие окна. Исправьте ошибку и перезапустите приложение",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                
            }
        }

        private async void GalleryWindow_Closed(object sender, EventArgs e)
        {
            if (galleryWindow != null)
            {
                galleryWindow.Closed -= GalleryWindow_Closed; // Отписываемся от события
                galleryWindow.Content = null;
                galleryWindow = null; // Освобождаем ссылку
            }

            await Task.Delay(300);
            // Принудительная очистка памяти
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public async Task ResetDesc()
        {
            Canvas.SetTop(CanvasDesc, 90);
            await Task.Delay(10);
        }
    }
}
