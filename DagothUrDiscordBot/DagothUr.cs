using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.PlayerSkillMonitor;
using DagothUrDiscordBot.StatFetcher;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;

namespace DagothUrDiscordBot;

class DagothUr
{
    private static DagothUr? instance;

    private readonly DiscordSocketClient client;
    private CommandManager.CommandManager commandManager;
    private PlayerSynchronizer synchronizer;

    // Store an array of GuildIDs and chat channel IDs
    // These will be channels where the bot can chat unprompted messages, such as level-up messages
    private Dictionary<ulong, ulong> guildDefaultChatChannels = new();
    private Dictionary<ulong, string> guildGroupIronmanNames = new();

    static void Main(String[] args)
    {
        instance = new DagothUr();
        instance.MainAsync()
            .GetAwaiter()
            .GetResult();
    }

    public static DagothUr GetInstance()
    {
        return instance!;
    }

    public DagothUr()
    {
        synchronizer = new PlayerSynchronizer();
        // Setup the privileges
        DiscordSocketConfig config = new DiscordSocketConfig
        {
            GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent
        };

        // Create the discord client
        client = new DiscordSocketClient(config);
        client.Log += OnLog;
        client.Ready += OnReady;

        commandManager = new CommandManager.CommandManager(client);
    }

    public void SetGuildGroupIronmanName(ulong guildID, string gimName)
    {
        guildGroupIronmanNames[guildID] = gimName;
    }

    public void SetGuildDefaultChatChannel(ulong guildID, ulong channelID)
    {
        guildDefaultChatChannels[guildID] = channelID;
    }

    public string? GetGuildGroupIronmanName(ulong guildID)
    {
        string? gimName;
        try
        {
            guildGroupIronmanNames.TryGetValue(guildID, out gimName);
        }catch(ArgumentNullException)
        {
            return null;
        }

        return gimName;
    }

    public ulong? GetGuildDefaultChatChannelID(ulong guildID)
    {
        ulong chatChannelID;
        try
        {
            guildDefaultChatChannels.TryGetValue(guildID, out chatChannelID);
        }
        catch (ArgumentNullException)
        {
            return null;
        }

        return chatChannelID;
    }

    public async Task MainAsync()
    {
        string discordBotToken = Environment.GetEnvironmentVariable("token") ?? string.Empty;
        await client.LoginAsync(TokenType.Bot, discordBotToken);

        // Starts the connection. Returns after one is established and runs the connection on another thread
        await client.StartAsync();

        // Setup a timer to recurringly check for player skill gains
        System.Timers.Timer playerSkillChangesTimer = new()
        {
            Interval = 3600 * 1000,
            AutoReset = true,
            Enabled = true
        };

        playerSkillChangesTimer.Elapsed += OnPlayerSkillCheckTimedEvent;

        // Block the program from closing until it is closed manually
        await Task.Delay(Timeout.Infinite);
    }

    public PlayerSynchronizer GetPlayerSynchronizer()
    {
        return this.synchronizer;
    }

    private async void OnPlayerSkillCheckTimedEvent(Object? source, System.Timers.ElapsedEventArgs e)
    {
        Console.WriteLine("Timer elapsed.");
        foreach (ulong guildID in this.guildGroupIronmanNames.Keys)
        {
            var guild = this.client.GetGuild(guildID);
            if (guild != null)
            {
                string gimName = this.guildGroupIronmanNames[guildID];
                ulong? chatChannelID = this.GetGuildDefaultChatChannelID(guildID);
                if (chatChannelID != null && chatChannelID != 0)
                {
                    var channel = guild.GetTextChannel(chatChannelID ?? 0);
                    if (channel != null)
                    {
                        string finalMessageToSend = ""; 
                        List<HighscoresPlayer> hsPlayers = await synchronizer.GetGIMHighscorePlayers(gimName);
                        foreach (HighscoresPlayer hsPlayer in hsPlayers)
                        {
                            List<ChangedSkill> changedSkills = synchronizer.GetPlayerChangedSkills(hsPlayer);
                            if (changedSkills.Count > 0)
                            {
                                finalMessageToSend += $"=== {hsPlayer.GetName()} Gains ===\n";
                                foreach(ChangedSkill s in changedSkills)
                                {
                                    if (s.DeltaLevel == 1)
                                    {
                                        finalMessageToSend += $"{s.Name.Trim()} increased by {s.DeltaLevel} level to {s.NewLevel}\n";
                                    }
                                    else
                                    {
                                        finalMessageToSend += $"{s.Name.Trim()} increased by {s.DeltaLevel} levels to {s.NewLevel}\n";
                                    }
                                    
                                }

                                Player player = synchronizer.GetPlayerFromDatabase(hsPlayer)!;
                                synchronizer.UpdatePlayerSkillsInDatabase(player.Id, hsPlayer);
                                finalMessageToSend += "\n";
                            }
                            else
                            {
                                Console.WriteLine($"{hsPlayer.GetName()} made no gains.");
                                // finalMessageToSend += $"== ${hsPlayer.GetName()} has not made any gains. ==\n";
                            }
                        }

                        if (!string.IsNullOrEmpty(finalMessageToSend))
                        {
                            await channel.SendMessageAsync(finalMessageToSend);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No channel in guild {guildID} with channel ID {chatChannelID}. This guild should probably rerun the set-default-chat-channel command.");
                    }
                }
                else
                {
                    Console.WriteLine($"Cannot check stats for GIM {gimName}. No default chat channel set!");
                }
            }
            else
            {
                Console.WriteLine($"Could not find a guild with ID {guildID}");
            }
            
            
        }
       
    }

    private Task OnLog(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task OnReady()
    {
        await commandManager.RegisterCommands();
    }
}