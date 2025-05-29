using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Turbo_Flapper
{
    public partial class Form1 : Form
    {
        private int pipeSpeed = 8;
        private int gravity = 10;
        private int score = 0;
        private int flapperInitialTop;
        private bool gameOver = false;
        private Random random = new Random();

        // Паттерн "Состояние"
        private GameState currentState = GameState.Menu;

        // Паттерн "Объектный пул"
        private PipePool pipePool;
        private List<Pipe> activePipes = new List<Pipe>();
        private int pipeSpawnTimer = 0;
        private const int pipeSpawnInterval = 1800 / 16; // Интервал в кадрах (1800 мс)
        private const int pipeGap = 200; // Фиксированный промежуток между трубами

        public Form1()
        {
            InitializeComponent();
            flapperInitialTop = flapper.Top;

            // Включение двойной буферизации для устранения мерцания
            this.DoubleBuffered = true;

            // Инициализация пула труб
            pipePool = new PipePool(
                new Size(pipeTop.Width, pipeTop.Height),
                new Size(pipeBottom.Width, pipeBottom.Height)
            );

            // Начальное состояние
            SetGameState(GameState.Menu);
        }

        private void SetGameState(GameState state)
        {
            currentState = state;
            switch (state)
            {
                case GameState.Menu:
                    startLabel.Visible = true;
                    scoreText.Visible = false;
                    flapper.Visible = false;
                    gameOver = true;
                    break;
                case GameState.Playing:
                    startLabel.Visible = false;
                    scoreText.Visible = true;
                    flapper.Visible = true;
                    gameOver = false;
                    RestartGame();
                    gameTimer.Start();
                    break;
                case GameState.GameOver:
                    gameTimer.Stop();
                    scoreText.Text += " Game Over! Press R to restart";
                    gameOver = true;
                    break;
            }
        }

        private void gameTimerEvent(object sender, EventArgs e)
        {
            if (currentState != GameState.Playing) return;

            // Обновление позиции птицы
            flapper.Top += gravity;

            // Обновление и проверка труб
            UpdatePipes();

            // Генерация новых труб
            pipeSpawnTimer++;
            if (pipeSpawnTimer >= pipeSpawnInterval)
            {
                SpawnPipes();
                pipeSpawnTimer = 0;
            }

            // Проверка столкновений
            CheckCollisions();

            // Увеличение скорости каждые 5 очков
            if (score % 5 == 0 && score != 0)
            {
                pipeSpeed += 1;
            }
        }

        private void UpdatePipes()
        {
            for (int i = activePipes.Count - 1; i >= 0; i--)
            {
                var pipe = activePipes[i];
                pipe.Position = new Point(pipe.Position.X - pipeSpeed, pipe.Position.Y);

                // Проверка выхода за пределы экрана
                if (pipe.Position.X < -pipe.Size.Width)
                {
                    pipePool.ReturnPipe(pipe);
                    activePipes.RemoveAt(i);
                    continue;
                }

                // Обновление счета
                if (pipe.Position.X + pipe.Size.Width < flapper.Left && !pipe.Passed)
                {
                    score++;
                    scoreText.Text = "Score: " + score;
                    pipe.Passed = true;
                }
            }

            // Перерисовка
            Invalidate();
        }

        private void SpawnPipes()
        {
            // Генерация позиции для верхней трубы
            int topY = random.Next(-539, -56);

            // Создание пары труб
            var topPipe = pipePool.GetPipe(true);
            var bottomPipe = pipePool.GetPipe(false);

            // Установка позиций
            topPipe.Position = new Point(this.ClientSize.Width, topY);
            bottomPipe.Position = new Point(
                this.ClientSize.Width,
                topY + topPipe.Size.Height + pipeGap
            );

            // Добавление в активный список
            activePipes.Add(topPipe);
            activePipes.Add(bottomPipe);
        }

        private void CheckCollisions()
        {
            // Проверка столкновения с землей
            if (flapper.Bounds.IntersectsWith(ground.Bounds) || flapper.Top < -25)
            {
                SetGameState(GameState.GameOver);
                return;
            }

            // Проверка столкновения с трубами
            foreach (var pipe in activePipes)
            {
                Rectangle pipeRect = new Rectangle(pipe.Position, pipe.Size);
                if (flapper.Bounds.IntersectsWith(pipeRect))
                {
                    SetGameState(GameState.GameOver);
                    return;
                }
            }
        }

        private void gameKeyDown(object sender, KeyEventArgs e)
        {
            if (currentState == GameState.Playing && e.KeyCode == Keys.Space)
            {
                gravity = -10;
            }
        }

        private void gameKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                gravity = 10;
            }
            else if (e.KeyCode == Keys.R && gameOver)
            {
                SetGameState(GameState.Playing);
            }
            else if (e.KeyCode == Keys.Enter && currentState == GameState.Menu)
            {
                SetGameState(GameState.Playing);
            }
        }

        private void RestartGame()
        {
            // Сброс параметров
            pipeSpeed = 8;
            gravity = 10;
            score = 0;
            pipeSpawnTimer = 0;
            scoreText.Text = "Score: 0";
            flapper.Top = flapperInitialTop;

            // Возврат всех труб в пул
            foreach (var pipe in activePipes)
            {
                pipePool.ReturnPipe(pipe);
            }
            activePipes.Clear();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentState == GameState.Playing)
            {
                // Отрисовка активных труб
                foreach (var pipe in activePipes)
                {
                    e.Graphics.DrawImage(pipe.IsTop ? Properties.Resources.pipedown : Properties.Resources.pipe,
                        new Rectangle(pipe.Position, pipe.Size));
                }
            }
        }
    }

    // Паттерн "Состояние"
    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }

    // Класс для представления трубы
    public class Pipe
    {
        public Point Position { get; set; }
        public Size Size { get; }
        public bool IsTop { get; }
        public bool Passed { get; set; }

        public Pipe(Size size, bool isTop)
        {
            Size = size;
            IsTop = isTop;
        }
    }

    // Паттерн "Объектный пул" для труб
    public class PipePool
    {
        private readonly Size topPipeSize;
        private readonly Size bottomPipeSize;
        private readonly Queue<Pipe> availablePipes = new Queue<Pipe>();

        public PipePool(Size topSize, Size bottomSize)
        {
            topPipeSize = topSize;
            bottomPipeSize = bottomSize;
        }

        public Pipe GetPipe(bool isTop)
        {
            if (availablePipes.Count > 0)
            {
                var pipe = availablePipes.Dequeue();
                pipe.Passed = false;
                return pipe;
            }

            return new Pipe(isTop ? topPipeSize : bottomPipeSize, isTop);
        }

        public void ReturnPipe(Pipe pipe)
        {
            availablePipes.Enqueue(pipe);
        }
    }
}
