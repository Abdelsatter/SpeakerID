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
        private static double EuclideanDistance(MFCCFrame frame1, MFCCFrame frame2)
        {
            double sum = 0.0f;
            for (int k = 0; k < 13; k++)
            {
                double diff = frame1.Features[k] - frame2.Features[k];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        public static double ComputeDTW(Sequence input, Sequence template)
        {
            if (input == null || input.Frames == null || input.Frames.Length == 0 ||
                template == null || template.Frames == null || template.Frames.Length == 0)
                throw new ArgumentException("Input and template sequences must be non-empty.");

            int N = input.Frames.Length;
            int M = template.Frames.Length;

            // Initialize arrays
            double[] prev = new double[M + 1];
            double[] curr = new double[M + 1];
            for (int j = 0; j <= M; j++)
            {
                prev[j] = double.PositiveInfinity;
                curr[j] = double.PositiveInfinity;
            }

            prev[0] = 0.0f;

            if (M >= 1)
                prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]);

            for (int i = 1; i <= N; i++)
            {
                for (int j = 0; j <= M; j++) curr[j] = double.PositiveInfinity;

                for (int j = 1; j <= M; j++)
                {
                    double cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]);
                    double minTrans = prev[j];
                    if (j >= 1)
                        minTrans = Math.Min(minTrans, prev[j - 1]);
                    if (j >= 2)
                        minTrans = Math.Min(minTrans, prev[j - 2]);

                    curr[j] = cost + minTrans;
                }

                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            double result = Math.Round(prev[M], 1);
            return result;
        }

        public static double ComputeDTW(Sequence input, Sequence template, int W)
        {
            if (input == null || input.Frames == null || input.Frames.Length == 0 ||
                template == null || template.Frames == null || template.Frames.Length == 0)
                throw new ArgumentException("Input and template sequences must be non-empty.");

            int N = input.Frames.Length;
            int M = template.Frames.Length;

            if (W != -1)
                W = Math.Max(W, 2 * Math.Abs(N - M));

            double[] prev = new double[M + 1];
            double[] curr = new double[M + 1];
            for (short j = 0; j <= M; j++)
            {
                prev[j] = double.PositiveInfinity;
                curr[j] = double.PositiveInfinity;
            }

            prev[0] = 0.0f;

            if (M >= 1)
                prev[1] = EuclideanDistance(input.Frames[0], template.Frames[0]);

            for (short i = 1; i <= N; i++)
            {
                short jStart = (short) (W == -1 ? 1 : Math.Max(1, i - W / 2)),
                    jEnd = (short) (W == -1 ? M : Math.Min(M, i + W / 2));

                for (short j = 0; j <= M; j++) curr[j] = double.PositiveInfinity;

                for (short j = jStart; j <= jEnd; j++)
                {
                    double cost = EuclideanDistance(input.Frames[i - 1], template.Frames[j - 1]),
                        minTrans = prev[j];

                    if (j >= 1)
                        minTrans = Math.Min(minTrans, prev[j - 1]);
                    if (j >= 2)
                        minTrans = Math.Min(minTrans, prev[j - 2]);

                    curr[j] = cost + minTrans;
                }

                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            return Math.Round(prev[M], 1);
        }
    }
}