using System;
using System.Collections.Generic;

namespace SkulAPMod.Helpers
{
    internal class Utils
    {
        // Source - https://stackoverflow.com/a
        // Posted by grenade, modified by community. See post 'Timeline' for change history
        // Retrieved 2025-12-16, License - CC BY-SA 4.0

        private static readonly Random rng = new Random();

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
