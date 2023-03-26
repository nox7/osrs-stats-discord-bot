using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.StatFetcher
{
    internal class HighscoresPlayer
    {
        private string name;
        private int totalLevel;
        private int totalXP;
        private List<HighscoresPlayerSkill> skillList;

        public HighscoresPlayer(string name, int totalLevel, int totalXP, List<HighscoresPlayerSkill> skillList)
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

        public List<HighscoresPlayerSkill> GetSkillList()
        {
            return this.skillList;
        }

        public string ToStringForDiscordWithoutSkills()
        {
            return $"**{this.name}**\n-- Total level: {this.totalLevel:n0}\n-- Total XP: {this.totalXP:n0}";
        }

        public HighscoresPlayerSkill? GetSkillByName(string name)
        {
            foreach (HighscoresPlayerSkill skill in skillList)
            {
                if (skill.GetName() == name)
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
