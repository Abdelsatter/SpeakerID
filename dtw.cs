using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Recorder.MFCC;

namespace Recorder
{
    public class DTW
    {
        // Computes Euclidean distance between two MFCCFrames
        private static float EuclideanDistance(MFCCFrame frame1, MFCCFrame frame2)
        {
            float sum = 0.0F;
            for (int k = 0; k < 13; k++)
            {
                double diff = frame1.Features[k] - frame2.Features[k];
                sum += (float)(diff * diff);
            }

            return (float)Math.Sqrt(sum);
        }

        public static float ComputeDTWAndCalcTime(Sequence input, Sequence template, int timeOutInMillisec = 10000)
        {
            float result = 0;
            long elapsedTime = 0;
            bool caseException = false;
            bool caseTimedOut = true;

            //Thread tstCaseThr = null;

            Stopwatch sw = new Stopwatch();

            //tstCaseThr = new Thread(() =>
            //{
            //    try
            //    {
            sw.Start();
            result = ComputeDTW(input, template);
            sw.Stop();
            //    }
            //    catch
            //    {
            //        caseException = true;
            //        result = float.MinValue;
            //    }
            //    caseTimedOut = false;
            //});

            //tstCaseThr.Start();
            //bool finishedInTime = tstCaseThr.Join(timeOutInMillisec);

            //if (!finishedInTime)
            //    result = float.MinValue;

            //if (!caseException && !caseTimedOut)
            elapsedTime = sw.ElapsedMilliseconds;

            Console.WriteLine("Elapsed time: " + elapsedTime + " ms");
            Console.WriteLine("Distance: " + result);
            return result;
        }

        // Computes DTW distance with pruning by limiting search paths
        public static float ComputeDTW(Sequence input, Sequence template, int W = -1)
        {
            if (input == null || input.Frames == null || input.Frames.Length == 0 ||
                template == null || template.Frames == null || template.Frames.Length == 0)
                throw new ArgumentException("Input and template sequences must be non-empty.");

            int N = input.Frames.Length;
            int M = template.Frames.Length;
            float[] prev = new float[M + 1];
            float[] curr = new float[M + 1];

            // Initialize arrays
            for (int j = 0; j <= M; j++)
            {
                prev[j] = float.PositiveInfinity;
                curr[j] = float.PositiveInfinity;
            }
            prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]); // Base case: (1, 1)

            // DP loop
            for (int i = 2; i <= N; i++)
            {
                // Reset curr array within window
                int jStart = W == -1 ? 1 : Math.Max(1, i - W);
                int jEnd = W == -1 ? M : Math.Min(M, i + W);

                for (int j = jStart; j <= jEnd; j++)
                {
                    float cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);
                    float minPrev = prev[j]; // (i-1, j)
                    if (j >= 2)
                        minPrev = Math.Min(minPrev, prev[j - 1]); // (i-1, j-1)
                    if (j >= 3)
                        minPrev = Math.Min(minPrev, prev[j - 2]); // (i-1, j-2)
                    curr[j] = cost + minPrev;
                }

                // Swap arrays
                float[] temp = prev;
                prev = curr;
                curr = temp;
            }

            return prev[M];
        }
    }
}
