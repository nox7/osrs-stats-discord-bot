using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.StatFetcher
{
    internal class PlayerSkill
    {
        private string name;
        private int level;
        private int xp;  

        public PlayerSkill(string name, int level, int xp) {
            this.name = name;
            this.level = level; 
            this.xp = xp; 
        }
    }
}
