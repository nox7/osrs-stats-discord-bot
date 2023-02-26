using Discord;
using Discord.WebSocket;

namespace DagothUrDiscordBot;

class DagothUr
{
    private readonly DiscordSocketClient client;
    private CommandManager.CommandManager commandManager;

    static void Main(String[] args)
    {
        // Load appsettings.json
        AppSettings.AppSettings.LoadAppSettingsFile();

        

        new DagothUr()
            .MainAsync()
            .GetAwaiter()
            .GetResult();
    }

    public DagothUr()
    {
        // Load app settings
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

    public async Task MainAsync()
    {
        await client.LoginAsync(TokenType.Bot, AppSettings.AppSettings.GetAppSetting("token"));

        // Starts the connection. Returns after one is established and runs the connection on another thread
        await client.StartAsync();

        // Block the program from closing until it is closed manually
        await Task.Delay(Timeout.Infinite);
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