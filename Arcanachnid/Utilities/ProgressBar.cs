using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Utilities
{
    public class ProgressBar : IDisposable
    {
        private const int blockCount = 10;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"/-\";
        private int animationIndex = 0;
        private Timer timer;

        public ProgressBar()
        {
            timer = new Timer(TimerHandler, null, animationInterval, animationInterval);
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                try
                {
                    Console.Write(animation[animationIndex++ % animation.Length]);

                    if (Console.CursorLeft > 0)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // Handle the case where the cursor position is outside the buffer.
                }
            }
        }
 
        public void Report(double progress)
        {
            progress = Math.Max(0, Math.Min(1, progress));

            int progressBlocks = (int)(progress * blockCount);
            int emptyBlocks = blockCount - progressBlocks;

            string visualProgress = new string('#', progressBlocks) + new string('-', emptyBlocks);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"[{visualProgress}] {progress:P1}");

            if (progress >= 1)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private bool disposed = false;
        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                timer.Dispose();
                Console.WriteLine();
            }
        }
    }
}
