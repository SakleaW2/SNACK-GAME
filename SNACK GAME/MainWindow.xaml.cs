using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SNACK_GAME
{
    public partial class MainWindow : Window
    {
        private List<Circle> Snake = new();
        private Circle food = new(), bonusFood = new();
        private Direction direction = Direction.Right;
        private DispatcherTimer gameTimer = new();
        private Random rand = new();
        private int cellSize = 40, score = 0, highScore = 0, speed = 10, bonusTimer = 0;
        private bool isPaused = false, isGameOver = false, bonusActive = false;

        public MainWindow()
        {
            InitializeComponent();

            GameCanvas.Width = 800;
            GameCanvas.Height = 600;

            GameBorder.Width = 800 + GameBorder.BorderThickness.Left + GameBorder.BorderThickness.Right;
            GameBorder.Height = 600 + GameBorder.BorderThickness.Top + GameBorder.BorderThickness.Bottom;

            this.PreviewKeyDown += MainWindow_KeyDown;
            this.Focusable = true;
            this.Focus();
            Keyboard.Focus(this);

            StartGame();
        }

        private void StartGame()
        {
            this.Focus();
            Keyboard.Focus(this);

            isGameOver = isPaused = false;
            Snake.Clear();
            Snake.Add(new Circle { X = 10, Y = 5 });

            LoadHighScore();

            direction = Direction.Right;
            gameTimer.Interval = TimeSpan.FromMilliseconds(1000 / speed);
            gameTimer.Tick -= GameLoop;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            GenerateFood();

            lblPause.Visibility = Visibility.Hidden;
            ScoreText.Text = "Очки: 0";
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (isGameOver || isPaused) return;

            MoveSnake();
            CheckCollision();
            CheckFood();

            if (bonusActive && --bonusTimer <= 0) bonusActive = false;

            Redraw();
        }

        private void MoveSnake()
        {
            for (int i = Snake.Count - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    switch (direction)
                    {
                        case Direction.Left: Snake[i].X--; break;
                        case Direction.Right: Snake[i].X++; break;
                        case Direction.Up: Snake[i].Y--; break;
                        case Direction.Down: Snake[i].Y++; break;
                    }

                    Snake[i].X = (Snake[i].X + (int)(GameCanvas.Width / cellSize)) % (int)(GameCanvas.Width / cellSize);
                    Snake[i].Y = (Snake[i].Y + (int)(GameCanvas.Height / cellSize)) % (int)(GameCanvas.Height / cellSize);
                }
                else
                {
                    Snake[i].X = Snake[i - 1].X;
                    Snake[i].Y = Snake[i - 1].Y;
                }
            }
        }

        private void CheckCollision()
        {
            for (int i = 1; i < Snake.Count; i++)
                if (Snake[0].X == Snake[i].X && Snake[0].Y == Snake[i].Y)
                {
                    GameOver();
                    return;
                }
        }

        private void CheckFood()
        {
            if (Snake[0].X == food.X && Snake[0].Y == food.Y)
            {
                Snake.Add(new Circle { X = Snake[^1].X, Y = Snake[^1].Y });
                ScoreText.Text = "Очки: " + ++score;
                if (score % 5 == 0 && speed < 25)
                    gameTimer.Interval = TimeSpan.FromMilliseconds(1000 / ++speed);

                if (!bonusActive && score >= 5 && rand.Next(10) == 0)
                {
                    bonusFood = new Circle
                    {
                        X = rand.Next((int)(GameCanvas.Width / cellSize)),
                        Y = rand.Next((int)(GameCanvas.Height / cellSize))
                    };
                    bonusActive = true;
                    bonusTimer = 100;
                }
                GenerateFood();
            }

            if (bonusActive && Snake[0].X == bonusFood.X && Snake[0].Y == bonusFood.Y)
            {
                for (int i = 0; i < 3; i++)
                    Snake.Add(new Circle { X = Snake[^1].X, Y = Snake[^1].Y });
                score += 5;
                ScoreText.Text = "Очки: " + score;
                bonusActive = false;
            }
        }

        private void GenerateFood()
        {
            do
            {
                food.X = rand.Next((int)(GameCanvas.Width / cellSize));
                food.Y = rand.Next((int)(GameCanvas.Height / cellSize));
            }
            while (Snake.Exists(s => s.X == food.X && s.Y == food.Y));
        }

        private void Redraw()
        {
            GameCanvas.Children.Clear();
            for (int i = 0; i < Snake.Count; i++)
            {
                Rectangle segment = new()
                {
                    Width = cellSize,
                    Height = cellSize,
                    Fill = (i == 0) ? Brushes.Yellow : Brushes.Green
                };
                Canvas.SetLeft(segment, Snake[i].X * cellSize);
                Canvas.SetTop(segment, Snake[i].Y * cellSize);
                GameCanvas.Children.Add(segment);
            }

            GameCanvas.Children.Add(new Ellipse
            {
                Width = cellSize,
                Height = cellSize,
                Fill = Brushes.Red,
                Margin = new Thickness(food.X * cellSize, food.Y * cellSize, 0, 0)
            });

            if (bonusActive)
            {
                GameCanvas.Children.Add(new Ellipse
                {
                    Width = cellSize,
                    Height = cellSize,
                    Fill = Brushes.Gold,
                    Margin = new Thickness(bonusFood.X * cellSize, bonusFood.Y * cellSize, 0, 0)
                });
            }
        }

        private void GameOver()
        {
            isGameOver = true;
            gameTimer.Stop();
            if (score > highScore)
            {
                highScore = score;
                SaveHighScore();
            }
            MessageBox.Show("Игра окончена! Очки: " + score);
            this.Focus();
            Keyboard.Focus(this);
        }

        private void LoadHighScore()
        {
            if (File.Exists("highscore.txt") && int.TryParse(File.ReadAllText("highscore.txt"), out int hs))
                highScore = hs;
            HighScoreText.Text = "Рекорд: " + highScore;
        }

        private void SaveHighScore()
        {
            File.WriteAllText("highscore.txt", highScore.ToString());
            HighScoreText.Text = "Рекорд: " + highScore;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up when direction != Direction.Down:
                    direction = Direction.Up;
                    break;
                case Key.Down when direction != Direction.Up:
                    direction = Direction.Down;
                    break;
                case Key.Left when direction != Direction.Right:
                    direction = Direction.Left;
                    break;
                case Key.Right when direction != Direction.Left:
                    direction = Direction.Right;
                    break;
                case Key.Space:
                    isPaused = !isPaused;
                    lblPause.Visibility = isPaused ? Visibility.Visible : Visibility.Hidden;
                    break;
                case Key.Enter:
                    StartGame();
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}