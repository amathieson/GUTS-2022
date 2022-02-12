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
    }
}
