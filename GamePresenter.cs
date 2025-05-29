using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Turbo_Flapper
{
    public class GamePresenter
    {
        private readonly IGameView _view;
        private readonly GameModel _model;
        private readonly Random _random = new Random();
        private int _pipeSpawnTimer;
        private const int PipeSpawnInterval = 1800 / 16;

        public GamePresenter(IGameView view, Size topPipeSize, Size bottomPipeSize)
        {
            _view = view;
            _model = new GameModel(topPipeSize, bottomPipeSize);

            
            _view.KeyDownEvent += OnKeyDown;
            _view.KeyUpEvent += OnKeyUp;
        }

        public void Initialize()
        {
            _model.FlapperInitialSize = _view.FlapperSize;
            _view.ShowMenu();
        }

        public void Update()
        {
            if (_view.CurrentState != GameState.Playing) return;

            // Обновление позиции птицы
            _view.FlapperTop += _model.Gravity;

            // Обновление труб и бонусов
            UpdatePipes();

            // Генерация новых труб
            _pipeSpawnTimer++;
            if (_pipeSpawnTimer >= PipeSpawnInterval)
            {
                SpawnPipes();
                _pipeSpawnTimer = 0;
            }

            // Проверка столкновений
            CheckCollisions();

            // Обновление скорости
            if (_model.Score % 5 == 0 && _model.Score != 0)
            {
                _model.PipeSpeed += 0.05f;
            }

            // Обновление отображения
            _view.UpdateGameElements();
        }

        private void StartGame()
        {
            _model.Reset();
            _view.ResetView();
            _view.GameOver = false;
            _view.CurrentState = GameState.Playing;
            _view.StartGame();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_view.CurrentState == GameState.Playing && e.KeyCode == Keys.Space)
            {
                _model.Gravity = -10;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _model.Gravity = 10;
            }
            else if (e.KeyCode == Keys.R && _view.GameOver)
            {
                StartGame();
            }
            else if (e.KeyCode == Keys.Enter && _view.CurrentState == GameState.Menu)
            {
                StartGame();
            }
        }

        private void UpdatePipes()
        {
            int newScore = _model.Score;
            for (int i = _model.ActivePipes.Count - 1; i >= 0; i--)
            {
                var pipe = _model.ActivePipes[i];
                pipe.Position = new Point(pipe.Position.X - (int)_model.PipeSpeed, pipe.Position.Y);

                // Обновление счета
                if (pipe.Position.X + pipe.Size.Width < _view.FlapperBounds.Left && !pipe.Passed)
                {
                    newScore++;
                    pipe.Passed = true;
                }

            }
            _model.Score = newScore;
            _view.ScoreText = "Score: " + _model.Score / 2;

            // Обновление позиций бонусов
            for (int i = _model.ActiveBonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = _model.ActiveBonuses[i];
                bonus.Position = new Point(bonus.Position.X - (int)_model.PipeSpeed, bonus.Position.Y);

                // Удаление бонусов, вышедших за экран
                if (bonus.Position.X + bonus.Size.Width < 0)
                {
                    _model.ActiveBonuses.RemoveAt(i);
                }
            }

            // Обновление длительности бонуса
            if (_model.ActiveBonusEffect != null)
            {
                _model.ActiveBonusDuration--;
                if (_model.ActiveBonusDuration <= 0)
                {
                    RemoveActiveBonusEffect();
                }
            }

            _view.DrawPipes(_model.ActivePipes);
            _view.DrawBonuses(_model.ActiveBonuses);
        }

        private void SpawnPipes()
        {
            // Генерация позиции для верхней трубы
            int topY = _random.Next(-539, -56);

            // Создание пары труб
            var topPipe = _model.PipePool.GetPipe(true);
            var bottomPipe = _model.PipePool.GetPipe(false);

            // Установка позиций
            topPipe.Position = new Point(_view.Width, topY);
            bottomPipe.Position = new Point(
                _view.Width,
                topY + topPipe.Size.Height + _model.PipeGap
            );

            _model.ActivePipes.Add(topPipe);
            _model.ActivePipes.Add(bottomPipe);

            // Генерация бонусов
            if (_random.NextDouble() < _model.BonusSpawnProbability)
            {
                BonusType type = (BonusType)_random.Next(0, 4);
                int minY = topY + topPipe.Size.Height + 25;
                int maxY = topY + topPipe.Size.Height + _model.PipeGap - 25;

                if (maxY > minY)
                {
                    int bonusY = _random.Next(minY, maxY);
                    Bonus bonus = new Bonus()
                    {
                        Position = new Point(_view.Width, bonusY),
                        Size = new Size(50, 50),
                        Type = type,
                        IsActive = true
                    };
                    _model.ActiveBonuses.Add(bonus);
                }
            }
        }

        private void CheckCollisions()
        {
            // Проверка столкновения с землей
            if (_view.FlapperBounds.IntersectsWith(_view.GroundBounds))
            {
                _view.EndGame("Game Over! Press R to restart");
                _view.GameOver = true;
                _view.CurrentState = GameState.GameOver;
                return;
            }

            // Проверка столкновения с трубами
            foreach (var pipe in _model.ActivePipes)
            {
                Rectangle pipeRect = new Rectangle(pipe.Position, pipe.Size);
                if (_view.FlapperBounds.IntersectsWith(pipeRect))
                {
                    _view.EndGame("Game Over! Press R to restart");
                    _view.GameOver = true;
                    _view.CurrentState = GameState.GameOver;
                    return;
                }
            }

            // Проверка сбора бонусов
            for (int i = _model.ActiveBonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = _model.ActiveBonuses[i];
                Rectangle bonusRect = new Rectangle(bonus.Position, bonus.Size);
                if (_view.FlapperBounds.IntersectsWith(bonusRect))
                {
                    ApplyBonusEffect(bonus.Type);
                    _model.ActiveBonuses.RemoveAt(i);
                    break;
                }
            }
        }

        private void ApplyBonusEffect(BonusType type)
        {
            // Отменяем предыдущий бонус
            RemoveActiveBonusEffect();

            _model.ActiveBonusEffect = type;
            _model.ActiveBonusDuration = 250; // Длительность 5 очков

            switch (type)
            {
                case BonusType.ReduceFlapperSize:
                    _view.FlapperSize = new Size(
                        (int)(_model.FlapperInitialSize.Width * 0.8),
                        (int)(_model.FlapperInitialSize.Height * 0.8)
                    );
                    break;

                case BonusType.IncreaseGap:
                    _model.PipeGap = _model.InitialPipeGap + 100;
                    break;

                case BonusType.IncreaseFlapperSize:
                    _view.FlapperSize = new Size(
                        (int)(_model.FlapperInitialSize.Width * 1.1),
                        (int)(_model.FlapperInitialSize.Height * 1.1)
                    );
                    break;

                case BonusType.DecreaseGap:
                    _model.PipeGap = _model.InitialPipeGap - 25;
                    break;
            }
        }

        private void RemoveActiveBonusEffect()
        {
            if (_model.ActiveBonusEffect == null) return;

            // Восстановление исходных параметров
            switch (_model.ActiveBonusEffect.Value)
            {
                case BonusType.ReduceFlapperSize:
                case BonusType.IncreaseFlapperSize:
                    _view.FlapperSize = _model.FlapperInitialSize;
                    break;

                case BonusType.IncreaseGap:
                case BonusType.DecreaseGap:
                    _model.PipeGap = _model.InitialPipeGap;
                    break;
            }

            _model.ActiveBonusEffect = null;
            _model.ActiveBonusDuration = 0;
        }
    }
}
