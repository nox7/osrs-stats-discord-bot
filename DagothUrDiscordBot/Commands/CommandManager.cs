﻿using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using DagothUrDiscordBot.OldSchoolHiscores;
using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;

namespace DagothUrDiscordBot.Commands
{
    internal class CommandManager
    {
        private DiscordSocketClient client;

        public CommandManager(DiscordSocketClient client)
        {
            this.client = client;

            client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task RegisterCommands()
        {

            List<ApplicationCommandProperties> slashCommandBuilders = new();

            SlashCommandBuilder statWatchCommand = new SlashCommandBuilder();
            statWatchCommand.WithName(SlashCommands.StatWatch);
            statWatchCommand.WithDescription("Sets an Old School RSN to monitor for stat changes.");
            statWatchCommand.AddOption("rsn", ApplicationCommandOptionType.String, "The name of the Old School Runescape account to monitor.", isRequired: true);

            SlashCommandBuilder removeRSNCommand = new SlashCommandBuilder();
            removeRSNCommand.WithName(SlashCommands.RemoveRSN);
            removeRSNCommand.WithDescription("Removes a player's RSN from being tracked.");
            removeRSNCommand.AddOption("rsn", ApplicationCommandOptionType.String, "The name of the Old School Runescape account to remove.", isRequired: true);

            SlashCommandBuilder getLastCheckTimeCommand = new SlashCommandBuilder();
            getLastCheckTimeCommand.WithName(SlashCommands.GetLastCheckTime);
            getLastCheckTimeCommand.WithDescription("Returns that last date-time that stats were checked.");

            SlashCommandBuilder fetchStatChangesCommand = new SlashCommandBuilder();
            fetchStatChangesCommand.WithName(SlashCommands.RunStatsCheck);
            fetchStatChangesCommand.WithDescription("Manually runs stats check to determine if any tracked players' stats have changed.");

            SlashCommandBuilder getTrackedRSNsCommand = new SlashCommandBuilder();
            getTrackedRSNsCommand.WithName(SlashCommands.GetTrackedRsns);
            getTrackedRSNsCommand.WithDescription("Informs you which Old School RSNs are being tracked currently.");

            SlashCommandBuilder setAsDefaultBotChatChannelCommand = new SlashCommandBuilder();
            setAsDefaultBotChatChannelCommand.WithName(SlashCommands.SetAsBotChannel);
            setAsDefaultBotChatChannelCommand.WithDescription("Sets this channel to receive the automatic player skill updates.");

            slashCommandBuilders.Add(statWatchCommand.Build());
            slashCommandBuilders.Add(removeRSNCommand.Build());
            slashCommandBuilders.Add(getLastCheckTimeCommand.Build());
            slashCommandBuilders.Add(fetchStatChangesCommand.Build());
            slashCommandBuilders.Add(getTrackedRSNsCommand.Build());
            slashCommandBuilders.Add(setAsDefaultBotChatChannelCommand.Build());

            DagothUr programInstance = DagothUr.GetInstance();

            if (programInstance.IsDevelopment())
            {
                string? testGuildId = Environment.GetEnvironmentVariable("testGuildId");
                if (string.IsNullOrEmpty(testGuildId))
                {
                    throw new Exception("Environment variable testGuildId is either null or empty. It must be present when running in a development environment.");
                }

                Console.WriteLine($"Development environment. Registering slash commands to test guild Id {testGuildId}");
                SocketGuild guild = client.GetGuild(Convert.ToUInt64(testGuildId));
                await guild.BulkOverwriteApplicationCommandAsync(slashCommandBuilders.ToArray());
            }
            else
            {
                Console.WriteLine("Environment is production. Registering slash commands to be global.");
                await client.BulkOverwriteGlobalApplicationCommandsAsync(slashCommandBuilders.ToArray());
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"Command '{command.Data.Name}' used.");
            ulong? guildId = command.GuildId;
            string commandName = command.Data.Name;

            if (guildId == null)
            {
                await command.RespondAsync("Commands for this application must be performed from within a guild.");
                return;
            }

            if (commandName == SlashCommands.StatWatch)
            {
                await command.DeferAsync();

                var rsnDataOption = command.Data.Options.Where(dataOption => dataOption.Name == "rsn").FirstOrDefault();
                if (rsnDataOption == null)
                {
                    await command.FollowupAsync("Please provide an RSN.");
                }
                else
                {
                    string rsn = rsnDataOption.Value.ToString()!;
                    // Subscribe a new RSN to have their stats monitored

                    var statWatchCommand = new StatWatchCommand();
                    bool isAlreadyMonitored = statWatchCommand.IsRSNMonitoredInGuild(rsn, (ulong)guildId);
                    if (isAlreadyMonitored)
                    {
                        await command.FollowupAsync($"{rsn} is already monitored in this server. If you want to check if their stats have changed without waiting for the next interval timer tick, then use the /{SlashCommands.RunStatsCheck} command instead.");
                    }
                    else
                    {
                        // Add them to the database
                        Debug.WriteLine($"Adding {rsn} to the local database and fetching their stats.");
                        Player? player = await statWatchCommand.AddRSNToBeMonitored(rsn, (ulong)guildId);

                        Debug.WriteLine("Player added to the local database.");
                        Debug.WriteLine("Fetching player's hiscore stats to store now.");
                        var statFetcher = new StatFetcher();
                        HiscoresPlayer? hsPlayer = await statFetcher.GetHiscoresPlayerFromRSN(rsn);
                        if (hsPlayer == null)
                        {
                            await command.FollowupAsync($"{rsn} was added to the tracking database, but their stats were not able to be fetched at this time. Is the Old School Runescape hiscores system down? I'll try again at a later time to fetch their stats. Don't worry.");
                        }
                        else
                        {
                            // Not null, store their stats
                            player!.UpdateSkills(hsPlayer.GetSkillList());
                            Debug.WriteLine("Player skills successfully added to the local database to be tracked.");
                            await command.FollowupAsync($"{rsn} successfully added to be tracked.");
                        }
                    }
                }
            }
            else if (commandName == SlashCommands.RunStatsCheck)
            {
                await command.DeferAsync();
                // Manual run of stat checks for a given guild ID
                var runStatCheckCommand = new RunStatCheckCommand();
                var changeTracker = new ChangeTracker();
                List<ChangedPlayer> playersWithChangedStats = await runStatCheckCommand.GetPlayersWithChangedSkills((ulong)guildId);
                if (playersWithChangedStats.Count > 0)
                {
                    List<Embed> embeds = new();
                    foreach(var changedPlayer in playersWithChangedStats)
                    {
                        embeds.Add(changedPlayer.GetDiscordEmbedRepresentingChanges());
                    }
                    await command.FollowupAsync(embeds: embeds.ToArray());
                }
                else
                {
                    await command.FollowupAsync("No tracked players have made any level gains.");
                }
            }
            else if (commandName == SlashCommands.SetAsBotChannel)
            {
                await command.DeferAsync();
                var setAsBotChannelCommand = new SetAsBotChannelCommand();
                setAsBotChannelCommand.RegisterChannelAsDefaultBotChannelForGuild((ulong)guildId, (ulong)command.ChannelId!);
                await command.FollowupAsync("This channel will now be used for all automatic player updates.");
            }
            else if (commandName == SlashCommands.GetTrackedRsns)
            {
                await command.DeferAsync();
                var getTrackedRSNsCommand = new GetTrackedRSNsCommand();
                var listOfPlayerNames = getTrackedRSNsCommand.GetTrackedRSNsForGuild((ulong)guildId);
                if (listOfPlayerNames.Count > 0)
                {
                    Embed embedOfPlayerNames = getTrackedRSNsCommand.GetEmbedOfTrackedPlayerrNames(listOfPlayerNames);
                    await command.FollowupAsync(embed: embedOfPlayerNames);
                }
                else
                {
                    await command.FollowupAsync("There are no players being tracked in this server.");
                }
            }
            else if (commandName == SlashCommands.GetLastCheckTime)
            {
                await command.DeferAsync();
                var lastCheckTimeCommand = new GetLastCheckTimeCommand();
                await command.FollowupAsync(embed: lastCheckTimeCommand.GetEmbedForLastCheckTimeAndNextCheckTime());
            }
            else if (commandName == SlashCommands.RemoveRSN)
            {
                await command.DeferAsync();
                var rsnDataOption = command.Data.Options.Where(dataOption => dataOption.Name == "rsn").FirstOrDefault();
                if (rsnDataOption == null)
                {
                    await command.FollowupAsync("Please provide an RSN.");
                }
                else
                {
                    string rsn = rsnDataOption.Value.ToString()!;
                    var removeRSNCommandClass = new RemoveRSNCommand();
                    bool didSucceed = removeRSNCommandClass.RemoveRSNFromGuildTracking(rsn, (ulong)guildId);
                    if (didSucceed)
                    {
                        await command.FollowupAsync($"{rsn} has been removed from the tracking system.");
                    }
                    else
                    {
                        await command.FollowupAsync($"{rsn} was never tracked. Nothing has been removed.");
                    }
                }
            }
            else
            {
                await command.RespondAsync("No idea what that is.");
            }
        }
    }
}
