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

        public CommandManager(DiscordSocketClient client) {
            this.client = client;

            client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task RegisterCommands()
        {
            SlashCommandBuilder setGimNameCommand = new SlashCommandBuilder();
            setGimNameCommand.WithName("set-gim-name");
            setGimNameCommand.WithDescription("Sets the group ironman name for the current Discord server");
            setGimNameCommand.AddOption("group-name", ApplicationCommandOptionType.String, "The name of the ironman group you want this bot to monitor in this server.", isRequired: true);

            SlashCommandBuilder getStatsCommand = new SlashCommandBuilder();
            getStatsCommand.WithName("get-stats");
            getStatsCommand.WithDescription("Fetches the stats for the GIM bois");

            SlashCommandBuilder setDefaultChatChannelCommand = new SlashCommandBuilder();
            setDefaultChatChannelCommand.WithName("set-default-chat-channel");
            setDefaultChatChannelCommand.WithDescription("Sets the default channel the bot will send unprompted updates to.");

            string testGuildID = Environment.GetEnvironmentVariable("testGuildID") ?? string.Empty;

            if (testGuildID != "")
            {
                System.Console.WriteLine($"testGuildID is defined ({testGuildID}). Registering slash commands to a specific guild.");
                SocketGuild guild = client.GetGuild(Convert.ToUInt64(testGuildID));
                await guild.CreateApplicationCommandAsync(getStatsCommand.Build());
                await guild.CreateApplicationCommandAsync(setGimNameCommand.Build());
                await guild.CreateApplicationCommandAsync(setDefaultChatChannelCommand.Build());
            }
            else
            {
                System.Console.WriteLine("testGuildID is null. Registering slash commands to be GLOBAL.");
                await client.CreateGlobalApplicationCommandAsync(getStatsCommand.Build());
                await client.CreateGlobalApplicationCommandAsync(setGimNameCommand.Build());
                await client.CreateGlobalApplicationCommandAsync(setDefaultChatChannelCommand.Build());
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"Command '{command.Data.Name}' used.");
            ulong? guildID = command.GuildId;

            if (command.Data.Name == "get-stats")
            {
                await command.DeferAsync();
                try
                {
                    string groupStatsMessage = await GetStatsCommand(guildID ?? 0);
                    await command.FollowupAsync(groupStatsMessage);
                }
                catch (HttpRequestException)
                {
                    await command.FollowupAsync("There was an HTTP error looking up the stats. Is the group name spelled correctly?");
                }
            }
            else if (command.Data.Name == "set-gim-name")
            {
                if (guildID != null)
                {
                    string gimName = command.Data.Options.First().Value.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(gimName)){
                        await command.RespondAsync("Your group name cannot be empty.");
                    }
                    else
                    {
                        // Try to lookup this GIM and synchronize the players into the DB
                        Console.WriteLine($"Receiving GIM Name {gimName}. Attempting to lookup and synchronize players.");
                        PlayerSynchronizer synchronizer = DagothUr.GetInstance().GetPlayerSynchronizer();
                        try
                        {
                            await command.DeferAsync();
                            await synchronizer.SynchronizePlayersIntoDatabase(gimName);
                            await command.FollowupAsync($"Set this server's monitored GIM to: {gimName}.");
                            DagothUr.GetInstance().SetGuildGroupIronmanName(guildID ?? 0, gimName);
                        }
                        catch (HttpRequestException)
                        {
                            await command.RespondAsync($"Failed to lookup stats and confirm the existence for the group: {gimName}. Did you spell it right?");
                            return;
                        }
                    }
                }
                else
                {
                    await command.RespondAsync("This command is only valid when used in a guild.");
                }
            }
            else if (command.Data.Name == "set-default-chat-channel")
            {
                ulong? channelID = command.ChannelId;
                if (guildID != null && channelID != null)
                {
                    DagothUr.GetInstance().SetGuildDefaultChatChannel(guildID ?? 0, channelID ?? 0);
                    await command.RespondAsync("Set this channel as the default channel for unprompted chats and level up messages.");
                }
                else
                {
                    await command.RespondAsync("This command is only valid when used in a guild and channel.");
                }
            }
            else
            {
                await command.RespondAsync("Unknown command.");
            }
        }

        private async Task<string> GetStatsCommand(ulong guildID)
        {
            string? gimName = DagothUr.GetInstance().GetGuildGroupIronmanName(guildID);
            if (gimName != null)
            {
                StatFetcher.StatFetcher statFetcher = new(gimName);
                string taskResult = await statFetcher.GetGroupStats();
                return taskResult;
            }
            else
            {
                return "You have not yet setup the group ironman name for this server. Run the setup command.";
            }
        }
    }
}
