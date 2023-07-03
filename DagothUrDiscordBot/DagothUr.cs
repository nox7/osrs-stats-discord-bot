using DagothUrDiscordBot.Commands;
using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using DagothUrDiscordBot.OldSchoolHiscores;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DagothUrDiscordBot;

class DagothUr
{
    private static DagothUr? instance;

    private readonly DiscordSocketClient client;
    private CommandManager commandManager;
    private string _runtimeEnvironment;
    private System.Timers.Timer? _skillChangeCheckTimer = null;
    private DateTime _lastTimerTick = DateTime.Now;
    private int _timerTickIntervalMs = 600000;

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

        string? runtimeEnvironment = Environment.GetEnvironmentVariable("RUNNING_ENVIRONMENT");
        if (runtimeEnvironment == null)
        {
            throw new Exception("Missing environment variable 'RUNNING_ENVIRONMENT' - Use a value of either 'Development' or 'Production'");
        }

        this._runtimeEnvironment = runtimeEnvironment;

        // Run migrations in Production
        if (this.IsProduction())
        {
            Console.WriteLine("Running database migrations.");
            using (var dbContext = new OSRSStatBotDbContext())
            {
                dbContext.Database.Migrate();
            }
        }

        // Setup the privileges
        DiscordSocketConfig config = new DiscordSocketConfig
        {
            GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent
        };

        // Create the discord client
        client = new DiscordSocketClient(config);
        client.Log += OnLog;
        client.Ready += OnReady;

        commandManager = new CommandManager(client);
    }

    /// <summary>
    /// Either 'Development' or 'Production'
    /// </summary>
    /// <returns></returns>
    public string GetRuntimeEnvironment()
    {
        return this._runtimeEnvironment;
    }

    public bool IsDevelopment()
    {
        return this.GetRuntimeEnvironment().ToLower() == "development";
    }

    public bool IsProduction()
    {
        return this.GetRuntimeEnvironment().ToLower() == "production";
    }

    public DateTime GetLastTimerTickDateTime()
    {
        return this._lastTimerTick;
    }

    public int GetSkillCheckTimerIntervalInMs()
    {
        return this._timerTickIntervalMs;
    }

    /// <summary>
    /// Fetches the default chat channel ID a Discord server has set for the bot to use to post updates in
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public ulong? GetBotChatChannelForGuild(ulong guildId)
    {
        using (var dbContext = new OSRSStatBotDbContext())
        {
            GuildDefaultChatChannel? chatChannel = dbContext.GuildDefaultChatChannels.Where(channel => channel.GuildId == guildId).FirstOrDefault();
            if (chatChannel != null)
            {
                return chatChannel.ChannelId;
            }
        }

        return null;
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
            Interval = _timerTickIntervalMs,
            AutoReset = false,
            Enabled = true
        };

        playerSkillChangesTimer.Elapsed += OnPlayerSkillCheckTimedEvent;
        _skillChangeCheckTimer = playerSkillChangesTimer;

        // Block the program from closing until it is closed manually
        await Task.Delay(Timeout.Infinite);
    }

    private async void OnPlayerSkillCheckTimedEvent(Object? source, System.Timers.ElapsedEventArgs e)
    {
        this._lastTimerTick = DateTime.Now;
        Console.WriteLine("Skill check timer ticked.");
        var statFetcher = new StatFetcher();

        // Fetch all servers that have given a default chat channel
        using (var dbContext = new OSRSStatBotDbContext())
        {
            // Handle 100 players at a time to avoid server strain on both our DB and Jagex's
            int currentPage = 1;
            int limit = 100;

            while(true)
            {
                List<Player> trackedPlayers = dbContext.Players.Skip((currentPage - 1) * limit).Take(limit).ToList();

                if (trackedPlayers.Count == 0)
                {
                    Debug.WriteLine($"Done reading tracked players in timer on page {currentPage}");
                    // Break when the list is empty
                    break;
                }

                // Lookup live stats
                foreach(Player dbPlayer in trackedPlayers)
                {
                    HiscoresPlayer? hsPlayer = await statFetcher.GetHiscoresPlayerFromRSN(dbPlayer.PlayerName);
                    if (hsPlayer != null)
                    {
                        List<ChangedSkill> changedSkills = dbPlayer.GetAnyChangedSkillsComparedToList(hsPlayer.GetSkillList());
                        if (changedSkills.Count > 0)
                        {
                            Debug.WriteLine($"{dbPlayer.PlayerName} has skill changes.");
                            // Has changed skills
                            dbContext.Attach(dbPlayer);
                            dbPlayer.LastDataFetchTimestamp = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            dbPlayer.TotalLevel = hsPlayer.GetTotalLevel();
                            dbPlayer.TotalXP = hsPlayer.GetTotalXP();

                            var changedPlayer = new ChangedPlayer();
                            changedPlayer.player = dbPlayer;
                            changedPlayer.changedSkills = changedSkills;

                            var embedForChangedPlayer = changedPlayer.GetDiscordEmbedRepresentingChanges();

                            // Update their stats in the DB
                            dbPlayer.UpdateSkills(hsPlayer.GetSkillList());

                            // Fetch the guild ID for this player
                            List<ulong> guildIDsPlayerIsMonitoredBy = PlayerGuildLink.GetAllGuildIdsPlayerIsMonitoredBy(dbPlayer.Id);

                            foreach(ulong guildId in guildIDsPlayerIsMonitoredBy)
                            {
                                SocketGuild guild = client.GetGuild(guildId);
                                Debug.WriteLine($"Found guild {guildId} for player {dbPlayer.PlayerName}.");

                                // Did they set a bot text channel?
                                ulong? channelId = GetBotChatChannelForGuild(guildId);
                                if (channelId != null)
                                {
                                    SocketTextChannel textChannel = guild.GetTextChannel((ulong)channelId);
                                    RestUserMessage responseMessage = await textChannel.SendMessageAsync(embed: embedForChangedPlayer);
                                    Debug.WriteLine($"Message sent for {guildId} and {channelId}.");
                                }
                                else
                                {
                                    Console.WriteLine($"Guild {guildId} did not set a default bot channel to post about {dbPlayer.PlayerName}'s skill updates.");
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"{dbPlayer.PlayerName} has no skill changes to post about.");
                        }
                    }
                    else
                    {
                        // TODO Handle failures with DB counter and remove from the DB if the counter has 3 failures
                        Console.WriteLine($"Error looking up {dbPlayer.PlayerName} - did their RSN change?");
                    }
                }

                dbContext.SaveChanges();
                ++currentPage;
            }

        }

        // After all done, start the timer again
        _skillChangeCheckTimer.Start();
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