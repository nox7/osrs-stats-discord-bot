using DagothUrDiscordBot.OldschoolHiscores;
using DagothUrDiscordBot.OldSchoolHiscores;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DagothUrDiscordBot.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }
        public int MemberId { get; set; }
        public int TotalLevel { get; set; }
        public long TotalXP { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string PlayerName { get; set; } = null!;
        public int LastDataFetchTimestamp { get; set; }

        /// <summary>
        /// Provides a list of skills that have a different level or virtual in the local DB than the provided list of HS skills for this player.
        /// This performs a check ONLY on the XP and not the level column - this way we can check virtual levels
        /// </summary>
        /// <param name="skillsListFromHiscores"></param>
        /// <returns></returns>
        public List<ChangedSkill> GetAnyChangedSkillsComparedToList(List<HiscoresPlayerSkill> skillsListFromHiscores)
        {
            var xpMap = new LevelXPMap();
            List<ChangedSkill> changedSkills = new();
            var currentSkillsInDb = GetAllSkills();

            foreach(var hsSkill in skillsListFromHiscores)
            {
                // Find the current skill In DB that matches this hiscore skill
                var dbSkill = currentSkillsInDb.Where(skillInDb => skillInDb.SkillName == hsSkill.GetName()).FirstOrDefault();
                if (dbSkill != null)
                {
                    // Get the locally stored skill level
                    int skillLevelInXB = dbSkill.SkillLevel;

                    // Get the skill level, possibly virtual (past 99) from the Hiscores XP
                    int virtualSkillLevelFromHS = xpMap.GetLevelFromXP(hsSkill.GetXP());
                    if (skillLevelInXB != virtualSkillLevelFromHS)
                    {
                        // It's a changed skill
                        var changedSkill = new ChangedSkill();
                        changedSkill.Name = hsSkill.GetName();
                        changedSkill.OldLevel = skillLevelInXB;
                        changedSkill.NewLevel = virtualSkillLevelFromHS;
                        changedSkills.Add(changedSkill);
                    }
                }
            }

            return changedSkills;
        }

        /// <summary>
        /// Updates all skills for this player with the provided HiscorePlayerSkill list. If the skill from the HS doesn't exist in the local DB
        /// for the Player instance, then it is created
        /// </summary>
        public void UpdateSkills(List<HiscoresPlayerSkill> skillsListFromHiscores)
        {
            var currentSkillsInDb = GetAllSkills();
            var levelXPMap = new LevelXPMap();

            using (var dbContext = new OSRSStatBotDbContext())
            {
                foreach(var hsSkill in skillsListFromHiscores)
                {
                    var existingSkill = currentSkillsInDb.Where(playerSkill => playerSkill.SkillName == hsSkill.GetName()).FirstOrDefault();
                    // Get the, possibly, virtual level from the XP to store in the local DB
                    int skillLevelFromHiscoresXP = levelXPMap.GetLevelFromXP(hsSkill.GetXP());
                    if (existingSkill != null)
                    {

                        // Manually change it
                        dbContext.PlayerSkills.Attach(existingSkill);
                        existingSkill.SkillXp = hsSkill.GetXP();
                        existingSkill.SkillLevel = skillLevelFromHiscoresXP;
                    }
                    else
                    {
                        // New skill that isn't found in the DB
                        var dbSkill = new PlayerSkill();
                        dbSkill.PlayerId = Id;
                        dbSkill.SkillName = hsSkill.GetName();
                        dbSkill.SkillXp = hsSkill.GetXP();
                        dbSkill.SkillLevel = skillLevelFromHiscoresXP;
                        dbContext.PlayerSkills.Add(dbSkill);
                    }
                }

                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Fetches all the local DB skills for this specific player. This DOES NOT get the current hiscore skills from the Old School Runescape
        /// database. Use the StatFetcher class for that.
        /// </summary>
        /// <returns></returns>
        public List<PlayerSkill> GetAllSkills()
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                return dbContext.PlayerSkills.Where(playerSkill => playerSkill.PlayerId == Id).ToList();
            }
        }
    }
}
