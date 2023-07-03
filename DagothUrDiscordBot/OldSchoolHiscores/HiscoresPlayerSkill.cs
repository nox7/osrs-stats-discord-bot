namespace DagothUrDiscordBot.OldschoolHiscores
{
    public class HiscoresPlayerSkill
    {
        private string name;
        private int level;
        private int xp;

        public HiscoresPlayerSkill(string name, int level, int xp)
        {
            this.name = name;
            this.level = level;
            this.xp = xp;
        }

        public string GetName() { return name; }
        public int GetLevel() { return level; }
        public int GetXP() { return xp; }
    }
}
