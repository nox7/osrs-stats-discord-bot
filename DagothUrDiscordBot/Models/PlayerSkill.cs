using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DagothUrDiscordBot.Models
{
    public class PlayerSkill
    {
        [Key]
        public int Id { get; set; }
        public int PlayerId { get; set; }
        [Column(TypeName = "varchar(255)")]
        public string SkillName { get; set; } = null!;
        public int SkillLevel { get; set; }
        public int SkillXp { get; set; }
    }
}
