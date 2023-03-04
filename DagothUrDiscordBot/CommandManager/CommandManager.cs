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

            string testGuildID = Environment.GetEnvironmentVariable("testGuildID") ?? string.Empty;

            if (testGuildID != "")
            {
                System.Console.WriteLine($"testGuildID is defined ({testGuildID}). Registering slash commands to a specific guild.");
                SocketGuild guild = client.GetGuild(Convert.ToUInt64(testGuildID));
                await guild.CreateApplicationCommandAsync(getStatsCommand.Build());
            }
            else
            {
                System.Console.WriteLine("testGuildID is null. Registering slash commands to be GLOBAL.");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"Command '{command.Data.Name}' used.");
            if (command.Data.Name == "get-stats")
            {
                await command.DeferAsync();
                string groupStatsMessage = await GetStatsCommand();
                await command.FollowupAsync(groupStatsMessage);
            }
            else
            {
                await command.RespondAsync("Unknown command.");
            }
        }

        private async Task<string> GetStatsCommand()
        {
            StatFetcher.StatFetcher statFetcher = new(Environment.GetEnvironmentVariable("gimName") ?? string.Empty);
            string taskResult = await statFetcher.GetGroupStats();
            return taskResult;
        }
    }
}
