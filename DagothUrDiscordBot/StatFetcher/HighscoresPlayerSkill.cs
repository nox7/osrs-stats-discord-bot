using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.StatFetcher
{
    internal class HighscoresPlayerSkill
    {
        private string name;
        private int level;
        private int xp;  

        public HighscoresPlayerSkill(string name, int level, int xp) {
            this.name = name;
            this.level = level; 
            this.xp = xp; 
        }

        public string GetName() { return name; }
        public int GetLevel() { return level; }
        public int GetXP() { return xp; }
    }
}
