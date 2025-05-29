using System;
using System.Collections.Generic;
using System.Drawing;

namespace Turbo_Flapper
{
    public class GameModel
    {
        public float PipeSpeed { get; set; } = 8f;
        public int Gravity { get; set; } = 10;
        public int Score { get; set; }
        public int PipeGap { get; set; } = 200;
        public bool GameOver { get; set; }
        public BonusType? ActiveBonusEffect { get; set; }
        public int ActiveBonusDuration { get; set; }

        public List<Pipe> ActivePipes { get; } = new List<Pipe>();
        public List<Bonus> ActiveBonuses { get; } = new List<Bonus>();
        public PipePool PipePool { get; }

        public Size FlapperInitialSize { get; set; }
        public int InitialPipeGap { get; } = 200;
        public double BonusSpawnProbability { get; } = 0.3;

        public GameModel(Size topPipeSize, Size bottomPipeSize)
        {
            PipePool = new PipePool(topPipeSize, bottomPipeSize);
            PipeGap = InitialPipeGap;
        }

        public void Reset()
        {
            PipeSpeed = 8f;
            Gravity = 10;
            Score = 0;
            PipeGap = InitialPipeGap;
            GameOver = false;
            ActivePipes.Clear();
            ActiveBonuses.Clear();
            ActiveBonusEffect = null;
            ActiveBonusDuration = 0;
        }
    }
}
