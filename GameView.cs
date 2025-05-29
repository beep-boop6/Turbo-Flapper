using System;
using System.Media;
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
    public partial class GameView : Form
    {
        private SoundPlayer gameOverSound;
        private float pipeSpeed = 8f;
        private int gravity = 10;
        private int score = 0;
        private int flapperInitialTop;
        private bool gameOver = false;
        private Random random = new Random();
        private Size topPipeSize;
        private Size bottomPipeSize;

        private List<Bonus> activeBonuses = new List<Bonus>();
        private Size bonusSize = new Size(50, 50);
        private double bonusSpawnProbability = 0.3; // 30% шанс появления бонуса
        private int initialPipeGap = 200; // сохраняем начальный промежуток
        private Size flapperInitialSize; // начальный размер птицы
        private BonusType? activeBonusEffect = null; // текущий активный бонус
        private int activeBonusDuration = 0; // длительность в очках

        // Паттерн "Состояние"
        private GameState currentState = GameState.Menu;

        // Паттерн "Объектный пул"
        private PipePool pipePool;
        private List<Pipe> activePipes = new List<Pipe>();
        private int pipeSpawnTimer = 0;
        private const int pipeSpawnInterval = 1800 / 16; // Интервал в кадрах (1800 мс)
        private int pipeGap = 200; // Фиксированный промежуток между трубами

        public GameView()
        {
            InitializeComponent();
            flapperInitialTop = flapper.Top;
            flapperInitialSize = flapper.Size;
            this.DoubleBuffered = true;

            // Сохраняем размеры труб
            topPipeSize = new Size(pipeTop.Width, pipeTop.Height);
            bottomPipeSize = new Size(pipeBottom.Width, pipeBottom.Height);

            // Инициализация пула труб с сохраненными размерами
            pipePool = new PipePool(topPipeSize, bottomPipeSize);


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
                pipeSpeed += 0.05f;
            }
        }

        private void UpdatePipes()
        {
            int newScore = score;
            for (int i = activePipes.Count - 1; i >= 0; i--)
            {
                var pipe = activePipes[i];
                pipe.Position = new Point(pipe.Position.X - (int)pipeSpeed, pipe.Position.Y);

                // Обновление счета
                if (pipe.Position.X + pipe.Size.Width < flapper.Left && !pipe.Passed)
                {
                    newScore++;
                    pipe.Passed = true;
                }
            }
            score = newScore;
            scoreText.Text = "Score: " + score / 2;

            // Обновление позиций бонусов
            for (int i = activeBonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = activeBonuses[i];
                bonus.Position = new Point(bonus.Position.X - (int)pipeSpeed, bonus.Position.Y);

                // Удаление бонусов, вышедших за экран
                if (bonus.Position.X + bonus.Size.Width < 0)
                {
                    activeBonuses.RemoveAt(i);
                }
            }

            // Обновление длительности бонуса
            if (activeBonusEffect != null)
            {
                activeBonusDuration--;
                if (activeBonusDuration <= 0)
                {
                    RemoveActiveBonusEffect();
                }
            }

            Invalidate();
        }

        private void SpawnPipes()
        {
            // Генерация позиции для верхней трубы
            int topY =  random.Next(-539, -56);

            // Создание пары труб
            var topPipe = pipePool.GetPipe(true);
            var bottomPipe = pipePool.GetPipe(false);

            // Установка позиций
            topPipe.Position = new Point(this.ClientSize.Width, topY);
            bottomPipe.Position = new Point(
                this.ClientSize.Width,
                topY + topPipe.Size.Height + pipeGap
            );

            if (random.NextDouble() < bonusSpawnProbability)
            {
                BonusType type = (BonusType)random.Next(0, 4);

                // Позиция бонуса - в промежутке между трубами
                int minY = topY + topPipeSize.Height + bonusSize.Height / 2;
                int maxY = topY + topPipeSize.Height + pipeGap - bonusSize.Height;

                if (maxY > minY) // Проверка, чтобы бонус поместился
                {
                    int bonusY = random.Next(minY, maxY);
                    Bonus bonus = new Bonus()
                    {
                        Position = new Point(this.ClientSize.Width, bonusY),
                        Size = bonusSize,
                        Type = type,
                        IsActive = true
                    };
                    activeBonuses.Add(bonus);
                }
            }
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

            for (int i = activeBonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = activeBonuses[i];
                Rectangle bonusRect = new Rectangle(bonus.Position, bonus.Size);
                if (flapper.Bounds.IntersectsWith(bonusRect))
                {
                    ApplyBonusEffect(bonus.Type);
                    activeBonuses.RemoveAt(i);
                    break;
                }
            }
        }

        private void ApplyBonusEffect(BonusType type)
        {
            // Отменяем предыдущий бонус
            RemoveActiveBonusEffect();

            activeBonusEffect = type;
            activeBonusDuration = 5; // Длительность 5 очков

            switch (type)
            {
                case BonusType.ReduceFlapperSize:
                    // Уменьшение размера птицы на 20%
                    flapper.Size = new Size(
                        (int)(flapperInitialSize.Width * 0.8),
                        (int)(flapperInitialSize.Height * 0.8)
                    );
                    break;

                case BonusType.IncreaseGap:
                    // Увеличение расстояния между трубами на 100px
                    pipeGap = initialPipeGap + 100;
                    break;

                case BonusType.IncreaseFlapperSize:
                    // Увеличение размера птицы на 10%
                    flapper.Size = new Size(
                        (int)(flapperInitialSize.Width * 1.1),
                        (int)(flapperInitialSize.Height * 1.1)
                    );
                    break;

                case BonusType.DecreaseGap:
                    // Уменьшение расстояния между трубами на 25px
                    pipeGap = initialPipeGap - 25;
                    break;
            }
        }

        private void RemoveActiveBonusEffect()
        {
            if (activeBonusEffect == null) return;

            // Восстановление исходных параметров
            switch (activeBonusEffect.Value)
            {
                case BonusType.ReduceFlapperSize:
                case BonusType.IncreaseFlapperSize:
                    flapper.Size = flapperInitialSize;
                    break;

                case BonusType.IncreaseGap:
                case BonusType.DecreaseGap:
                    pipeGap = initialPipeGap;
                    break;
            }

            activeBonusEffect = null;
            activeBonusDuration = 0;
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
            activeBonuses.Clear();
            RemoveActiveBonusEffect();
            pipeGap = initialPipeGap;
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

                foreach (var bonus in activeBonuses)
                {
                    Color color = Color.Gold;
                    switch (bonus.Type)
                    {
                        case BonusType.ReduceFlapperSize: color = Color.Blue; break;
                        case BonusType.IncreaseGap: color = Color.Green; break;
                        case BonusType.IncreaseFlapperSize: color = Color.Red; break;
                        case BonusType.DecreaseGap: color = Color.Purple; break;
                    }

                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        e.Graphics.FillEllipse(brush,
                            bonus.Position.X, bonus.Position.Y,
                            bonus.Size.Width, bonus.Size.Height);
                    }
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

    public class Bonus
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
        public BonusType Type { get; set; }
        public bool IsActive { get; set; }
    }

    public enum BonusType
    {
        ReduceFlapperSize,    // Уменьшение размера птицы
        IncreaseGap,         // Увеличение расстояния между трубами
        IncreaseFlapperSize,  // Увеличение размера птицы
        DecreaseGap          // Уменьшение расстояния между трубами
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
