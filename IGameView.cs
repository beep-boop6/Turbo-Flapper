using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Turbo_Flapper
{
    public interface IGameView
    {
        // Свойства
        int Width { get; }
        int FlapperTop { get; set; }
        Size FlapperSize { get; set; }
        string ScoreText { get; set; }
        bool GameOver { get; set; }
        GameState CurrentState { get; set; }
        Rectangle FlapperBounds { get; }
        Rectangle GroundBounds { get; }

        // События
        event EventHandler<KeyEventArgs> KeyDownEvent;
        event EventHandler<KeyEventArgs> KeyUpEvent;

        // Методы
        void ShowMenu();
        void StartGame();
        void EndGame(string message);
        void ResetView();
        void UpdateGameElements();
        void DrawPipes(List<Pipe> pipes);
        void DrawBonuses(List<Bonus> bonuses);
    }
}