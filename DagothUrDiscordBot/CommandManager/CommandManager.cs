using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.CommandManager
{
    internal class CommandManager
    {
        private DiscordSocketClient client;

        public CommandManager(DiscordSocketClient client) {
            this.client = client;

            client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task RegisterCommands()
        {
            SlashCommandBuilder getStatsCommand = new SlashCommandBuilder();
            getStatsCommand.WithName("get-stats");
            getStatsCommand.WithDescription("Fetches the stats for the GIM bois");

            if (Environment.GetEnvironmentVariable("testGuildID") != null)
            {
                System.Console.WriteLine("testGuildID is defined. Registering slash commands to a specific guild.");
                SocketGuild guild = client.GetGuild(Convert.ToUInt64(Environment.GetEnvironmentVariable("testGuildID")));
                await guild.CreateApplicationCommandAsync(getStatsCommand.Build());
            }
            else
            {
                System.Console.WriteLine("testGuildID is null. Registering slash commands to be GLOBAL.");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            await command.RespondAsync($"You executed {command.Data.Name}");
        }
    }
}
