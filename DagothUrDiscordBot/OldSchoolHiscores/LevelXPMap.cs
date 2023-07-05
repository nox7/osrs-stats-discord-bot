using AngleSharp.Html.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.OldSchoolHiscores
{
    public class LevelXPMap
    {
        /// <summary>
        /// Fetches the experience needed to reach the provided level. Make sure to check for 200,000,000 mil XP before using this.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public int GetXPForLevel(int level)
        {
            if (level == 0) return 0;
            if (level == 1) return 0;

            double accumulator = 0;
            for (int l = 1; l <= level-1; l++) {
                accumulator += Math.Floor(l + 300.0 * Math.Pow(2, l / 7.0));
            }
            return (int) Math.Floor(1.0 / 4.0 * accumulator);
        }

        public int GetLevelFromXP(int xp)
        {
            int lastLevelIterated = 1;
            if (xp == 0) return 1;
            for (int level = 2; level <= 126; level++)
            {
                int xpAtLevel = GetXPForLevel(level);

                if (xpAtLevel > xp)
                {
                    break;
                }
                lastLevelIterated = level;
            }

            return lastLevelIterated;
        }
    }
}
