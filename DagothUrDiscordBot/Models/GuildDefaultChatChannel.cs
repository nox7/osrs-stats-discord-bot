using System.ComponentModel.DataAnnotations;

namespace DagothUrDiscordBot.Models
{
    public class GuildDefaultChatChannel
    {
        [Key]
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
