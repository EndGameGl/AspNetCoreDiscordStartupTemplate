using AspNetCoreDiscordStartupTemplate.Options;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace AspNetCoreDiscordStartupTemplate.Services.Hosted;

public class DiscordStartupService : BackgroundService
{
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;

    public DiscordStartupService(
        DiscordShardedClient discordShardedClient,
        IOptions<DiscordBotOptions> discordBotOptions)
    {
        _discordShardedClient = discordShardedClient;
        _discordBotOptions = discordBotOptions;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}