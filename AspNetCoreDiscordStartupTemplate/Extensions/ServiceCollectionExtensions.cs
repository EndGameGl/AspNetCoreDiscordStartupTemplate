using AspNetCoreDiscordStartupTemplate.Options;
using AspNetCoreDiscordStartupTemplate.Services.Hosted;
using Discord.WebSocket;

namespace AspNetCoreDiscordStartupTemplate.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscord(
        this IServiceCollection serviceCollection,
        Action<DiscordSocketConfig> configure,
        IConfiguration configuration)
    {
        var config = new DiscordSocketConfig()
        {
        };

        var discordClient = new DiscordShardedClient(config);

        serviceCollection.Configure<DiscordBotOptions>(configuration.GetSection("DiscordBot"));

        serviceCollection.AddHostedService<DiscordStartupService>();
        
        return serviceCollection.AddSingleton(discordClient);
    }
}