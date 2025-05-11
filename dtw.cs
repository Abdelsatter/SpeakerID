using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recorder.MFCC;

namespace Recorder
{
    public class DTW
    {
        // Computes Euclidean distance between two MFCCFrames
        private static double EuclideanDistance(MFCCFrame frame1, MFCCFrame frame2)
        {
            double sum = 0.0;
            for (int k = 0; k < 13; k++)
            {
                double diff = frame1.Features[k] - frame2.Features[k];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        // Computes DTW distance between input and template sequences without pruning
        public static double ComputeDTW(Sequence input, Sequence template)
        {
            if (input == null || input.Frames == null || input.Frames.Length == 0 ||
                template == null || template.Frames == null || template.Frames.Length == 0)
                throw new ArgumentException("Input and template sequences must be non-empty.");

            int N = input.Frames.Length; // Length of input sequence
            int M = template.Frames.Length; // Length of template sequence

            // Initialize DP table with infinity
            double[,] dp = new double[N + 1, M + 1];
            for (int i = 0; i <= N; i++)
                for (int j = 0; j <= M; j++)
                    dp[i, j] = double.PositiveInfinity;

            // Base case: align first frames
            dp[1, 1] = EuclideanDistance(input.Frames[0], template.Frames[0]);

            // Fill DP table
            for (int i = 2; i <= N; i++)
            {
                for (int j = 1; j <= M; j++)
                {
                    double cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);

                    // Possible transitions:
                    // 1. (i-1, j) -> (i, j) : Stay at template frame j
                    // 2. (i-1, j-1) -> (i, j) : Move to next template frame
                    // 3. (i-1, j-2) -> (i, j) : Skip one template frame
                    double minPrev = dp[i - 1, j]; // Transition from (i-1, j)
                    if (j >= 2)
                        minPrev = Math.Min(minPrev, dp[i - 1, j - 1]); // From (i-1, j-1)
                    if (j >= 3)
                        minPrev = Math.Min(minPrev, dp[i - 1, j - 2]); // From (i-1, j-2)

                    dp[i, j] = cost + minPrev;
                }
            }

            // Return DTW distance
            return dp[N, M];
        }
    }
}
