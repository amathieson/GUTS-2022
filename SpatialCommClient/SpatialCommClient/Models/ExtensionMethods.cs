using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialCommClient.Models
{
    static class ExtensionMethods
    {
        public static T[] DequeueMany<T>(this Queue<T> queue, int n)
        {
            T[] ret = new T[n];
            for (int i = 0; i < n; i++)
                ret[i] = queue.Dequeue();
            return ret;
        }
        public static void EnqueueMany<T>(this Queue<T> queue, T[] data, int n)
        {
            for (int i = 0; i < n; i++)
                queue.Enqueue(data[i]);
        }

        /// <summary>
        /// Reverses the contents of a span and returns it. WARNING: This mutates the span!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <returns>The same span but reversed</returns>
        public static Span<T> ReverseSpan<T>(this Span<T> span)
        {
            span.Reverse();
            return span;
        }

        public static double Frac(double x)
        {
            double whole = Math.Truncate(x);
            return x - whole;
        }

        public static string ToStringFormatted(this OpenTK.Mathematics.Vector3 vec)
        {
            return $"[{vec.X:F3},  {vec.Y:F3},  {vec.Z:F3}]";
        }
    }
}
