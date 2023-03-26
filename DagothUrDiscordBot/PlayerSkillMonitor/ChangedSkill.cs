using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.PlayerSkillMonitor
{
    internal class ChangedSkill
    {
        public string Name { get; set; }
        public int DeltaLevel{ get; set; }
        public int NewLevel{ get; set; }

        public ChangedSkill(string name, int deltaLevel, int newLevel)
        {
            Name = name;
            DeltaLevel = deltaLevel;
            NewLevel = newLevel;
        }
    }
}
