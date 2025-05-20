using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Recorder.MFCC;

namespace Recorder
{
    public class DTW
    {
        private static float EuclideanDistance(MFCCFrame frame1, MFCCFrame frame2)
        {
            float sum = 0.0f; // Use double for precision
            for (int k = 0; k < 13; k++)
            {
                float diff = (float)(frame1.Features[k] - frame2.Features[k]);
                sum += diff * diff;
            }
            return (float)Math.Sqrt(sum);
        }

        public static float ComputeDTW(Sequence input, Sequence template, int W = -1)
        {
            if (input == null || input.Frames == null || input.Frames.Length == 0 ||
                template == null || template.Frames == null || template.Frames.Length == 0)
                throw new ArgumentException("Input and template sequences must be non-empty.");

            int N = input.Frames.Length;
            int M = template.Frames.Length;

            // Adjust window size
            if (W != -1)
            {
                W = Math.Max(W, 2 * Math.Abs(N - M));
                Console.WriteLine($"Adjusted W: {W}");
            }

            // Initialize arrays
            float[] prev = new float[M + 1];
            float[] curr = new float[M + 1];
            for (int j = 0; j <= M; j++)
            {
                prev[j] = float.PositiveInfinity;
                curr[j] = float.PositiveInfinity;
            }

            prev[0] = 0.0f;
            if (M >= 1)
            {
                prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]);
            }

            // Main DTW loop
            for (int i = 1; i <= N; i++)
            {
                int jStart = W == -1 ? 1 : Math.Max(1, i - W / 2);
                int jEnd = W == -1 ? M : Math.Min(M, i + W / 2);

                // Reset curr array
                for (int j = 0; j <= M; j++)
                {
                    curr[j] = float.PositiveInfinity;
                }

                // Compute DTW costs
                for (int j = jStart; j <= jEnd; j++)
                {
                    float cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);
                    float minTransition = prev[j]; // (i-1, j)
                    if (j >= 1)
                        minTransition = Math.Min(minTransition, prev[j - 1]); // (i-1, j-1)
                    if (j >= 2)
                        minTransition = Math.Min(minTransition, prev[j - 2]); // (i-1, j-2)

                    curr[j] = cost + minTransition;
                }

                // Log for debugging
                //Console.WriteLine($"i={i}, jStart={jStart}, jEnd={jEnd}, curr[M]={curr[M]}");

                // Swap arrays
                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            float result = prev[M];
            //Console.WriteLine($"Final DTW Distance: {result}");
            return result;
        }

        //public static float ComputeDTW(Sequence input, Sequence template, int W = -1)
        //{
        //    if (input == null || input.Frames == null || input.Frames.Length == 0 ||
        //        template == null || template.Frames == null || template.Frames.Length == 0)
        //        throw new ArgumentException("Input and template sequences must be non-empty.");

        //    int N = input.Frames.Length;
        //    int M = template.Frames.Length;

        //    // Adjust W: -1 means no pruning, otherwise ensure endpoint reachability
        //    if (W != -1)
        //    {
        //        W = Math.Max(W, Math.Abs(N - M)); // Minimum to reach (N,M)
        //    }

        //    // Two arrays for space optimization
        //    float[] prev = new float[M + 1];
        //    float[] curr = new float[M + 1];

        //    // Initialize arrays
        //    for (int j = 0; j <= M; j++)
        //    {
        //        prev[j] = float.PositiveInfinity;
        //        curr[j] = float.PositiveInfinity;
        //    }

        //    // Base case: align first frames
        //    prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]);

        //    // Main DTW loop
        //    for (int i = 2; i <= N; i++)
        //    {
        //        // Define pruning window
        //        int jStart = (W == -1) ? 1 : Math.Max(1, i - W);
        //        int jEnd = (W == -1) ? M : Math.Min(M, i + W);

        //        // Only compute within window
        //        for (int j = jStart; j <= jEnd; j++)
        //        {
        //            float cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);
        //            float minPrev = prev[j]; // (i-1,j) -> (i,j)
        //            if (j >= 2) minPrev = Math.Min(minPrev, prev[j - 1]); // (i-1,j-1) -> (i,j)
        //            if (j >= 3) minPrev = Math.Min(minPrev, prev[j - 2]); // (i-1,j-2) -> (i,j)
        //            curr[j] = cost + minPrev;
        //        }

        //        // Swap arrays
        //        float[] temp = prev;
        //        prev = curr;
        //        curr = temp;

        //        // Reset curr for next iteration
        //        for (int j = 0; j <= M; j++)
        //        {
        //            if (j < jStart || j > jEnd) curr[j] = float.PositiveInfinity;
        //        }
        //    }

        //    return prev[M];
        //}
    }
}
