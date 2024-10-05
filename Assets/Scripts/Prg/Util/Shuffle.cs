using System;
using System.Collections.Generic;
using System.Linq;

namespace Prg.Util
{
    /// <summary>
    /// Fisher-Yates shuffle
    /// https://jamesshinevar.medium.com/shuffle-a-list-c-fisher-yates-shuffle-32833bd8c62d
    /// </summary>
    public static class ShuffleExtension
    {
        public static List<T> ShuffleArray<T>(this List<T> list, int iterations = 1, Random random = null)
        {
            var array = list.ToArray();
            ShuffleArray(array, iterations, random);
            return array.ToList();
        }

        private static void ShuffleArray<T>(T[] arr, int iterations = 1, Random random = null)
        {
            random ??= new Random();
            for (var i = 0; i < iterations; i++)
            {
                var rnd = random.Next(0, arr.Length);
                (arr[rnd], arr[0]) = (arr[0], arr[rnd]);
            }
        }
    }
}
