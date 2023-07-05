namespace DagothUrDiscordBot.OldschoolHiscores
{
    public class HiscoresPlayer
    {
        private string name;
        private int totalLevel;
        private long totalXP;
        private List<HiscoresPlayerSkill> skillList = new();

        public HiscoresPlayer(string name)
        {
            this.name = name;
        }

        public string GetName()
        {
            return this.name;
        }

        public void SetTotalLevel(int totalLevel)
        {
            this.totalLevel = totalLevel;
        }

        public void SetTotalXP(long totalXP)
        {
            this.totalXP = totalXP;
        }

        public int GetTotalLevel()
        {
            return this.totalLevel;
        }

        public long GetTotalXP()
        {
            return this.totalXP;
        }

        public void AddSkill(HiscoresPlayerSkill skill)
        {
            this.skillList.Add(skill);
        }

        public List<HiscoresPlayerSkill> GetSkillList()
        {
            return this.skillList;
        }

        public HiscoresPlayerSkill? GetSkillByName(string name)
        {
            foreach (HiscoresPlayerSkill skill in skillList)
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
