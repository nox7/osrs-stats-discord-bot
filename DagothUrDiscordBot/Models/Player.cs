using System;
using System.Collections.Generic;

namespace DagothUrDiscordBot.Models;

public partial class Player
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public string PlayerName { get; set; } = null!;

    public int LastDataFetchTimestamp { get; set; }
}
