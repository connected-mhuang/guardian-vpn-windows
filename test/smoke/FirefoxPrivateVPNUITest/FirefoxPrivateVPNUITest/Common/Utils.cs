// <copyright file="Utils.cs" company="Mozilla">
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, you can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

namespace FirefoxPrivateVPNUITest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// Here are some utilities.
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Random select an index from integers based on the condition.
        /// </summary>
        /// <param name="range">A range of integers.</param>
        /// <param name="condition">A condition method.</param>
        /// <returns>A random index.</returns>
        public static int RandomSelectIndex(IEnumerable<int> range, Func<int, bool> condition)
        {
            var result = range.Where(condition);
            var rand = new Random();
            int index = rand.Next(0, result.Count());
            return result.ElementAt(index);
        }

        /// <summary>
        /// Remove all space, tab, new line character from multiline string.
        /// </summary>
        /// <param name="text">A string text.</param>
        /// <returns>A new string text.</returns>
        public static string CleanText(string text)
        {
            return Regex.Replace(text, @"[\r\n\t\s]+", string.Empty);
        }

        /// <summary>
        /// Wait until the file exists.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="timeOut">The timeout.</param>
        /// <returns>File exists or not.</returns>
        public static bool WaitUntilFileExist(string path, double timeOut = 10000)
        {
            Stopwatch time = new Stopwatch();
            time.Start();
            bool retry = true;
            bool exist = false;
            while (retry && time.ElapsedMilliseconds <= timeOut)
            {
                exist = File.Exists(path);
                if (exist)
                {
                    retry = false;
                    time.Stop();
                }
                else
                {
                    retry = true;
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }
            }

            if (time.ElapsedMilliseconds > timeOut && !exist)
            {
                time.Stop();
            }

            time.Reset();
            return exist;
        }
    }
}
