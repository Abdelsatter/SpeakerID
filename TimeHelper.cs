using System;
using System.Threading.Tasks;
using System.Diagnostics;


namespace Recorder
{
    public static class TimingHelper
    {
        public static T MeasureExecutionTimeAsync<T>(Func<T> func, string title)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            T result = func();
            sw.Stop();

            Console.WriteLine("Elapsed time for " + title + " is: " + sw.ElapsedMilliseconds + " ms");
            return result;
        }
    }

}
