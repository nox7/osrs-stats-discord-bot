using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.StatFetcher
{
    internal class Player
    {
        private string name;
        private int totalLevel;
        private int totalXP;
        private List<PlayerSkill> skillList;

        public Player(string name, int totalLevel, int totalXP, List<PlayerSkill> skillList)
        {
            this.name = name;
            this.totalLevel = totalLevel;
            this.totalXP = totalXP;
            this.skillList = skillList;
        }

        public int GetTotalLevel()
        {
            return this.totalLevel;
        }

        public string GetName()
        {
            return this.name;
        }

        public string ToStringForDiscordWithoutSkills()
        {
            return $"**{this.name}**\n-- Total level: {this.totalLevel:n0}\n-- Total XP: {this.totalXP:n0}";
        }
    }
}
