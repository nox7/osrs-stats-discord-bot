using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DagothUrDiscordBot.Models;

public partial class PlayerSkill
{
    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("skill_name")]
    [StringLength(125)]
    public string SkillName { get; set; } = null!;

    [Column("skill_level")]
    public int SkillLevel { get; set; }

    [Column("skill_xp")]
    public int SkillXp { get; set; }

    [Key]
    public int Id { get; set; }

    [ForeignKey("PlayerId")]
    [InverseProperty("PlayerSkills")]
    public virtual Player Player { get; set; } = null!;
}
