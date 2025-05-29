using System;
using System.Collections.Generic;
using System.Drawing;

namespace Turbo_Flapper
{
    public class Bonus
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
        public BonusType Type { get; set; }
        public bool IsActive { get; set; }
    }

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
