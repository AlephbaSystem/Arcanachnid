using System.Diagnostics;

namespace Arcanachnid.Utilities
{
    public class ProgressBar : IDisposable
    {
        private const int blockCount = 10;
        private readonly object lockObject = new object();
        private bool disposed = false;
        private Stopwatch stopwatch;

        public ProgressBar()
        {
            Console.CursorVisible = false;
            stopwatch = Stopwatch.StartNew();
        }

        public void Report(double progress)
        {
            lock (lockObject)
            {
                if (disposed) return;

                progress = Math.Max(0, Math.Min(1, progress));
                int progressBlocks = (int)(progress * blockCount);
                string visualProgress = new string('#', progressBlocks) + new string('-', blockCount - progressBlocks);

                string elapsedTime = FormatElapsedTime(stopwatch.Elapsed);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{elapsedTime} [{visualProgress}] {progress:P1}     ");

                if (progress >= 1)
                {
                    Console.CursorVisible = true;
                    Console.WriteLine();
                }
            }
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (!disposed)
                {
                    disposed = true;
                    stopwatch.Stop();
                    Console.CursorVisible = true;
                    Console.WriteLine();
                }
            }
        }

        private string FormatElapsedTime(TimeSpan elapsed)
        {
            return $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
    }
}