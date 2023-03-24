using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.PlayerSkillMonitor;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.CommandManager
{
    internal class CommandManager
    {
        private DiscordSocketClient client;
        private DagothUrContext database;

        public CommandManager(DiscordSocketClient client, DagothUrContext database) {
            this.client = client;
            this.database = database;

            client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task RegisterCommands()
        {
            SlashCommandBuilder getStatsCommand = new SlashCommandBuilder();
            getStatsCommand.WithName("get-stats");
            getStatsCommand.WithDescription("Fetches the stats for the GIM bois");

            SlashCommandBuilder loadPlayersIntoDB = new SlashCommandBuilder();
            getStatsCommand.WithName("load-players-into-db");
            getStatsCommand.WithDescription("Loads the Group Ironman players into the bot's database, if they are not already there.");

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
                await client.CreateGlobalApplicationCommandAsync(getStatsCommand.Build());
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
            else if (command.Data.Name == "load-players-into-db")
            {
                await command.DeferAsync();
                string responseMessage = await LoadPlayersIntoDB();
                await command.FollowupAsync(responseMessage);
            }
            else
            {
                await command.RespondAsync("Unknown command.");
            }
        }

        private async Task<string> GetStatsCommand()
        {
            StatFetcher.StatFetcher statFetcher = new(
                Environment.GetEnvironmentVariable("gimName") ?? string.Empty
            );
            string taskResult = await statFetcher.GetGroupStats();
            return taskResult;
        }

        private async Task<string> LoadPlayersIntoDB()
        {
            var synchronizer = new PlayerSkillSynchronizer(this.database);
            await synchronizer.SynchronizePlayersIntoDatabase();
            return "Loaded.";
        }
    }
}
