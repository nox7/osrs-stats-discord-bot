using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DagothUrDiscordBot.Models;

public partial class Player
{
    [Column("member_id")]
    public int MemberId { get; set; }

    [Column("player_name")]
    [StringLength(128)]
    public string PlayerName { get; set; } = null!;

    [Column("last_data_fetch_timestamp")]
    public int LastDataFetchTimestamp { get; set; }

    [Key]
    public int Id { get; set; }

    [InverseProperty("Player")]
    public virtual ICollection<PlayerSkill> PlayerSkills { get; } = new List<PlayerSkill>();
}
