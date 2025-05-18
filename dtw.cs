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
            float sum = 0.0F;
            for (int k = 0; k < 13; k++)
            {
                double diff = frame1.Features[k] - frame2.Features[k];
                sum += (float)(diff * diff);
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
            float[] prev = new float[M + 1];
            float[] curr = new float[M + 1];

            for (int j = 0; j <= M; j++)
            {
                prev[j] = float.PositiveInfinity;
                curr[j] = float.PositiveInfinity;
            }
            prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]);

            for (int i = 2; i <= N; i++)
            {
                int jStart = W == -1 ? 1 : Math.Max(1, i - W);
                int jEnd = W == -1 ? M : Math.Min(M, i + W);

                for (int j = jStart; j <= jEnd; j++)
                {
                    float cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);
                    float minPrev = prev[j];
                    if (j >= 2)
                        minPrev = Math.Min(minPrev, prev[j - 1]);
                    if (j >= 3)
                        minPrev = Math.Min(minPrev, prev[j - 2]);
                    curr[j] = cost + minPrev;
                    //prev[j] = curr[j];
                }

                float[] temp = prev;
                prev = curr;
                curr = temp;
            }

            return prev[M];
        }
    }
}
