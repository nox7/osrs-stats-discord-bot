using DagothUrDiscordBot.Commands;
using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using DagothUrDiscordBot.OldSchoolHiscores;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DagothUrDiscordBot;

class DagothUr
{
    private static DagothUr? ApplicationInstance;

    private readonly DiscordSocketClient DiscordClient;
    private CommandManager CommandManager;
    private string RuntimeEnvironment;
    private System.Timers.Timer? SkillChangeCheckTimer = null;
    private DateTime LastTimerTick = DateTime.Now;
    private int TimerTickIntervalMs = 600000;

    static void Main(String[] args)
    {
        ApplicationInstance = new DagothUr();
        ApplicationInstance.MainAsync()
            .GetAwaiter()
            .GetResult();
    }

    public static DagothUr GetInstance()
    {
        return ApplicationInstance!;
    }

    public DagothUr()
    {

        string? runtimeEnvironment = Environment.GetEnvironmentVariable("RUNNING_ENVIRONMENT");
        if (runtimeEnvironment == null)
        {
            throw new Exception("Missing environment variable 'RUNNING_ENVIRONMENT' - Use a value of either 'Development' or 'Production'");
        }

        RuntimeEnvironment = runtimeEnvironment;

        // Run migrations in Production
        if (IsProduction())
        {
            Console.WriteLine("Running database migrations.");
            using var dbContext = new OSRSStatBotDbContext();
            dbContext.Database.Migrate();
        }

        // Setup the privileges
        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        // Create the discord client
        DiscordClient = new DiscordSocketClient(config);
        DiscordClient.Log += OnLog;
        DiscordClient.Ready += OnReady;

        CommandManager = new CommandManager(DiscordClient);
    }

    /// <summary>
    /// Either 'Development' or 'Production'
    /// </summary>
    /// <returns></returns>
    public string GetRuntimeEnvironment()
    {
        return RuntimeEnvironment;
    }

    public bool IsDevelopment()
    {
        return GetRuntimeEnvironment().ToLower() == "development";
    }

    public bool IsProduction()
    {
        return GetRuntimeEnvironment().ToLower() == "production";
    }

    /// <summary>
    /// The last DateTime the timer for checking stats ticked.
    /// </summary>
    /// <returns></returns>
    public DateTime GetLastTimerTickDateTime()
    {
        return LastTimerTick;
    }

    /// <summary>
    /// Returns the millisecond interval that the skill check timer ticks at
    /// </summary>
    /// <returns></returns>
    public int GetSkillCheckTimerIntervalInMs()
    {
        return TimerTickIntervalMs;
    }

    /// <summary>
    /// Fetches the default chat channel ID a Discord server has set for the bot to use to post updates in
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public async Task<ulong?> GetBotChatChannelForGuild(ulong guildId)
    {
        using var dbContext = new OSRSStatBotDbContext();
        GuildDefaultChatChannel? chatChannel = await dbContext.GuildDefaultChatChannels
            .Where(channel => channel.GuildId == guildId)
            .FirstOrDefaultAsync();

        return chatChannel?.ChannelId;
    }

    /// <summary>
    /// Starts the Discord client. Begins the timer for checking player changed events.
    /// </summary>
    /// <returns></returns>
    public async Task MainAsync()
    {
        string discordBotToken = Environment.GetEnvironmentVariable("token") ?? string.Empty;
        await DiscordClient.LoginAsync(TokenType.Bot, discordBotToken);

        // Starts the connection. Returns after one is established and runs the connection on another thread
        await DiscordClient.StartAsync();

        // Setup a timer to recurringly check for player skill gains
        System.Timers.Timer playerSkillChangesTimer = new()
        {
            Interval = TimerTickIntervalMs,
            AutoReset = false,
            Enabled = true
        };

        playerSkillChangesTimer.Elapsed += OnPlayerSkillCheckTimedEvent;
        SkillChangeCheckTimer = playerSkillChangesTimer;

        // Block the program from closing until it is closed manually
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// Fires when the skill check timer elapses.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private async void OnPlayerSkillCheckTimedEvent(Object? source, System.Timers.ElapsedEventArgs e)
    {
        Console.WriteLine("Skill check timer ticked.");
        LastTimerTick = DateTime.Now;
        var statFetcher = new StatFetcher();

        // Fetch all servers that have given a default chat channel
        using var dbContext = new OSRSStatBotDbContext();

        // Handle 100 players at a time to avoid server strain on both our DB and Jagex's
        int currentPage = 1;
        int limit = 100;

        while(true)
        {
            List<Player> trackedPlayers = await dbContext.Players
                .Skip((currentPage - 1) * limit)
                .Take(limit)
                .ToListAsync();

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
                        dbPlayer.LastDataFetchTimestamp = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        dbPlayer.TotalLevel = hsPlayer.GetTotalLevel();
                        dbPlayer.TotalXP = hsPlayer.GetTotalXP();

                        var changedPlayer = new ChangedPlayer
                        {
                            player = dbPlayer,
                            changedSkills = changedSkills
                        };

                        var embedForChangedPlayer = changedPlayer.GetDiscordEmbedRepresentingChanges();

                        // Update their stats in the DB
                        dbPlayer.UpdateSkills(hsPlayer.GetSkillList());

                        // Fetch the guild ID for this player
                        List<ulong> guildIDsPlayerIsMonitoredBy = PlayerGuildLink.GetAllGuildIdsPlayerIsMonitoredBy(dbPlayer.Id);

                        foreach(ulong guildId in guildIDsPlayerIsMonitoredBy)
                        {
                            SocketGuild guild = DiscordClient.GetGuild(guildId);
                            Debug.WriteLine($"Found guild {guildId} for player {dbPlayer.PlayerName}.");

                            // Did they set a bot text channel?
                            ulong? channelId = await GetBotChatChannelForGuild(guildId);
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

            await dbContext.SaveChangesAsync();
            ++currentPage;
        }

        // After all done, start the timer again
        SkillChangeCheckTimer!.Start();
    }

    /// <summary>
    /// Fires when the Discord client logs a message.
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    private Task OnLog(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fires when the Discord client has successfully started and is ready to process commands and receive
    /// data.
    /// </summary>
    /// <returns></returns>
    private async Task OnReady()
    {
        await CommandManager.RegisterCommands();
    }
}