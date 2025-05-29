using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Turbo_Flapper
{
    public partial class GameForm : Form, IGameView
    {
        private int flapperInitialTop;
        public int Width => this.ClientSize.Width;
        public PictureBox PipeTop => pipeTop;
        public PictureBox PipeBottom => pipeBottom;
        public Timer GameTimer => gameTimer;
        private Size flapperInitialSize;
        private List<Pipe> pipesToDraw = new List<Pipe>();
        private List<Bonus> bonusesToDraw = new List<Bonus>();

        public GameForm()
        {
            InitializeComponent();
            this.KeyDown += (s, e) => KeyDownEvent?.Invoke(s, e);
            this.KeyUp += (s, e) => KeyUpEvent?.Invoke(s, e);
            flapperInitialTop = flapper.Top;
            flapperInitialSize = flapper.Size;
            this.DoubleBuffered = true;
        }

        // Реализация IGameView
        public int FlapperTop
        {
            get => flapper.Top;
            set => flapper.Top = value;
        }

        public Size FlapperSize
        {
            get => flapper.Size;
            set => flapper.Size = value;
        }

        public string ScoreText
        {
            get => scoreText.Text;
            set => scoreText.Text = value;
        }

        public bool GameOver { get; set; }
        public GameState CurrentState { get; set; } = GameState.Menu;

        public Rectangle FlapperBounds => flapper.Bounds;
        public Rectangle GroundBounds => ground.Bounds;

        public event EventHandler StartGameRequested;
        public event EventHandler RestartGameRequested;
        public event EventHandler<KeyEventArgs> KeyDownEvent;
        public event EventHandler<KeyEventArgs> KeyUpEvent;

        public void ShowMenu()
        {
            startLabel.Visible = true;
            scoreText.Visible = false;
            flapper.Visible = false;
        }

        public void StartGame()
        {
            startLabel.Visible = false;
            scoreText.Visible = true;
            flapper.Visible = true;
            gameTimer.Start();
        }

        public void EndGame(string message)
        {
            ScoreText += " " + message;
            gameTimer.Stop();
        }

        public void ResetView()
        {
            flapper.Top = flapperInitialTop;
            flapper.Size = flapperInitialSize;
            ScoreText = "Score: 0";
            pipesToDraw.Clear();
            bonusesToDraw.Clear();
            Invalidate();
        }

        public void UpdateGameElements()
        {
            // Отрисовка будет в методе OnPaint
            Invalidate();
        }

        public void DrawPipes(List<Pipe> pipes)
        {
            pipesToDraw = pipes;
        }

        public void DrawBonuses(List<Bonus> bonuses)
        {
            bonusesToDraw = bonuses;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Отрисовка труб
            foreach (var pipe in pipesToDraw)
            {
                Image pipeImage = pipe.IsTop ? Properties.Resources.pipedown : Properties.Resources.pipe;
                e.Graphics.DrawImage(pipeImage, new Rectangle(pipe.Position, pipe.Size));
            }

            // Отрисовка бонусов
            foreach (var bonus in bonusesToDraw)
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

        // Обработчики событий таймера и клавиатуры теперь в презентере
        private void gameTimerEvent(object sender, EventArgs e) { }
        private void gameKeyDown(object sender, KeyEventArgs e) { }
        private void gameKeyUp(object sender, KeyEventArgs e) { }

        private void GameForm_Load(object sender, EventArgs e)
        {

        }
    }
}
