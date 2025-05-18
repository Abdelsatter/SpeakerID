using System;
using System.Threading.Tasks;
using System.Diagnostics;


namespace Recorder
{
    public static class TimingHelper
    {
        public static T ExecutionTime<T>(Func<T> func, string label)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            T result = func();
            sw.Stop();

            Console.WriteLine($"{label} took {sw.ElapsedMilliseconds} ms");
            return result;
        }

        public static void ExecutionTime(Action action, string label)
        {
            Stopwatch sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            Console.WriteLine($"{label} took {sw.ElapsedMilliseconds} ms");
        }
    }

}
