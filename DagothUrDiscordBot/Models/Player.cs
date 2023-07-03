using DagothUrDiscordBot.OldschoolHiscores;
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
        public int TotalXP { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string PlayerName { get; set; } = null!;
        public int LastDataFetchTimestamp { get; set; }

        /// <summary>
        /// Provides a list of skills that have a different level in the local DB than the provided list of HS skills for this player.
        /// </summary>
        /// <param name="skillsListFromHiscores"></param>
        /// <returns></returns>
        public List<ChangedSkill> GetAnyChangedSkillsComparedToList(List<HiscoresPlayerSkill> skillsListFromHiscores)
        {
            List<ChangedSkill> changedSkills = new();
            var currentSkillsInDb = GetAllSkills();

            foreach(var hsSkill in skillsListFromHiscores)
            {
                // Find the current skill In DB that matches this hiscore skill
                var dbSkill = currentSkillsInDb.Where(skillInDb => skillInDb.SkillName == hsSkill.GetName()).FirstOrDefault();
                if (dbSkill != null)
                {
                    if (dbSkill.SkillLevel != hsSkill.GetLevel())
                    {
                        // It's a changed skill
                        var changedSkill = new ChangedSkill();
                        changedSkill.Name = hsSkill.GetName();
                        changedSkill.OldLevel = dbSkill.SkillLevel;
                        changedSkill.NewLevel = hsSkill.GetLevel();
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

            using (var dbContext = new OSRSStatBotDbContext())
            {
                foreach(var hsSkill in skillsListFromHiscores)
                {
                    var existingSkill = currentSkillsInDb.Where(playerSkill => playerSkill.SkillName == hsSkill.GetName()).FirstOrDefault();
                    if (existingSkill != null)
                    {
                        // Manually change it
                        dbContext.PlayerSkills.Attach(existingSkill);
                        existingSkill.SkillXp = hsSkill.GetXP();
                        existingSkill.SkillLevel = hsSkill.GetLevel();
                    }
                    else
                    {
                        // New skill that isn't found in the DB
                        var dbSkill = new PlayerSkill();
                        dbSkill.PlayerId = Id;
                        dbSkill.SkillName = hsSkill.GetName();
                        dbSkill.SkillXp = hsSkill.GetXP();
                        dbSkill.SkillLevel = hsSkill.GetLevel();
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
