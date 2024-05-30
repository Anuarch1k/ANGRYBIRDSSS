using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class MainWindow : Window

    {
    private bool isDragging; // взята ли птица клавишей в руку?
    private Point startPoint; // создание точки

    private double birdVelocityX;
    private double birdVelocityY;

    private const double Gravity = 1;
    private const double MaxDistance = 1900; // Максимальное расстояние, на которое птица может лететь
    private const double PowerScale = 0.1; // Масштаб для силы запуска птицы

    private Random random = new Random();

    private double slingshotStartPositionX;
    private double slingshotStartPositionY;

    private double birdStartPositionX;
    private double birdStartPositionY;

    private double pigStartPositionX;
    private double pigStartPositionY;

    private double coverStartPositionX;
    private double coverStartPositionY;

    private Polyline birdPath; // Путь птицы

    public MainWindow()
    {
        InitializeComponent();

        // Привязка обработчиков событий для рогатки
        Slingshot.MouseLeftButtonDown += Slingshot_MouseLeftButtonDown;
        Slingshot.MouseMove += Slingshot_MouseMove;
        Slingshot.MouseLeftButtonUp += Slingshot_MouseLeftButtonUp;

        // Привязка обработчика события для кнопки Restart
        restartButton.Click += RestartGame_Click;

        slingshotStartPositionX = Canvas.GetLeft(Slingshot);
        slingshotStartPositionY = Canvas.GetTop(Slingshot);

        birdStartPositionX = Canvas.GetLeft(Bird);
        birdStartPositionY = Canvas.GetTop(Bird);

        pigStartPositionX = Canvas.GetLeft(Pig);
        pigStartPositionY = Canvas.GetTop(Pig);

        coverStartPositionX = Canvas.GetLeft(Cover);
        coverStartPositionY = Canvas.GetTop(Cover);

        // Создание пути птицы
        birdPath = new Polyline
        {
            Stroke = Brushes.PaleVioletRed,
            StrokeThickness = 2
        };

        // Добавление пути птицы на холст
        GameCanvas.Children.Add(birdPath);
    }

    private void Slingshot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        isDragging = true;
        startPoint = e.GetPosition(GameCanvas);
        Slingshot.CaptureMouse();
    }

    private void Slingshot_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            Point currentPosition = e.GetPosition(GameCanvas);
            double offsetX = currentPosition.X - startPoint.X;
            double offsetY = currentPosition.Y - startPoint.Y;

            Canvas.SetLeft(Slingshot, Canvas.GetLeft(Slingshot) + offsetX);
            Canvas.SetTop(Slingshot, Canvas.GetTop(Slingshot) + offsetY);

            startPoint = currentPosition;
        }
    }

    private void Slingshot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging)
        {
            isDragging = false;
            Slingshot.ReleaseMouseCapture(); //отпустить захват мышь не захватывает объект

            LaunchBird(); //вызываем метод 
        }
    }

    private void LaunchBird() // запуск птицы функция такой
    {
        double birdX = Canvas.GetLeft(Bird);
        double birdY = Canvas.GetTop(Bird);
        double slingshotX = Canvas.GetLeft(Slingshot);
        double slingshotY = Canvas.GetTop(Slingshot);

        double offsetX = birdX - slingshotX;
        double offsetY = birdY - slingshotY;

        double distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
        double power = distance * PowerScale; // сила запуска пьтцы умножить 

        double directionX = offsetX / distance; //направление по икс
        double directionY = offsetY / distance;

        birdVelocityX = directionX * power;
        birdVelocityY = directionY * power;

        // Расчет угла полета птицы
        double angle = Math.Atan2(birdVelocityY, birdVelocityX) * 180 / Math.PI;
        Bird.RenderTransform = new RotateTransform(angle); // разворот птицы на угол

        CompositionTarget.Rendering += GameLoop; //для изобажения 

        double velocity = Math.Sqrt(birdVelocityX * birdVelocityX + birdVelocityY * birdVelocityY);

        angleText.Text = "Angle: " + angle.ToString("F2");
        velocityText.Text = "Velocity: " + velocity.ToString("F2");
    }

    private void GameLoop(object sender, EventArgs e)
    {
        // Обновление позиции птицы
        double birdX = Canvas.GetLeft(Bird) + birdVelocityX;
        double birdY = Canvas.GetTop(Bird) + birdVelocityY + Gravity;

        Canvas.SetLeft(Bird, birdX);
        Canvas.SetTop(Bird, birdY);

        // Добавление текущей позиции птицы в путь
        birdPath.Points.Add(new Point(birdX + Bird.Width / 2,
            birdY + Bird.Height / 2)); // чтобы мы рисовались траекториб всередиге

        // Проверка столкновений
        CheckCollision();

        // Проверка, достигла ли птица максимального расстояния или упала вниз
        if (birdY > GameCanvas.ActualHeight || birdX > GameCanvas.ActualWidth || birdX < 0 ||
            Math.Abs(birdX - Canvas.GetLeft(Slingshot)) > MaxDistance)
        {
            CompositionTarget.Rendering -= GameLoop;
            MessageBox.Show("Птица не достигла своей цели! Попробуйте еще раз.");
            ResetBird();
        }

        // Обновление скорости птицы с учетом гравитации
        birdVelocityY += Gravity;
    }

    private void ResetBird()
    {
        Canvas.SetLeft(Bird, birdStartPositionX);
        Canvas.SetTop(Bird, birdStartPositionY);
        birdVelocityX = 0;
        birdVelocityY = 0;

        // Очистка пути птицы
        birdPath.Points.Clear();

        // Возврат рогатки в изначальное положение
        Canvas.SetLeft(Slingshot, slingshotStartPositionX);
        Canvas.SetTop(Slingshot, slingshotStartPositionY);
    }

    private void CheckCollision()
    {
        Rect birdRect = new Rect(Canvas.GetLeft(Bird), Canvas.GetTop(Bird), Bird.Width, Bird.Height); // 
        Rect pigRect = new Rect(Canvas.GetLeft(Pig), Canvas.GetTop(Pig), Pig.Width, Pig.Height);
        Rect coverRect = new Rect(Canvas.GetLeft(Cover), Canvas.GetTop(Cover), Cover.Width, Cover.Height);

        if (birdRect.IntersectsWith(pigRect))
        {
            // Птица столкнулась с свиньей
            CompositionTarget.Rendering -= GameLoop;
            MessageBox.Show("Поздравляем! Вы попали в свинью!");
            ResetBird();
        }

        if (birdRect.IntersectsWith(coverRect))
        {
            // Птица столкнулась с преградой
            birdVelocityX = -birdVelocityX;
            birdVelocityY = -birdVelocityY;

            // Случайное смещение преграды при столкновении
            double offsetX = random.Next(-10, 10);
            double offsetY = random.Next(-10, 10);
            Canvas.SetLeft(Cover, Canvas.GetLeft(Cover) + offsetX);
            Canvas.SetTop(Cover, Canvas.GetTop(Cover) + offsetY);
        }
    }

    private void RestartGame_Click(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= GameLoop;
        ResetBird();

        // Очистка преград и свиней от случайного смещения
        Canvas.SetLeft(Cover, coverStartPositionX);
        Canvas.SetTop(Cover, coverStartPositionY);
        Canvas.SetLeft(Pig, pigStartPositionX);
        Canvas.SetTop(Pig, pigStartPositionY);

        LaunchBird();
    }
    }
}
