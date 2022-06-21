using AspNetCoreDiscordStartupTemplate.Options;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace AspNetCoreDiscordStartupTemplate.Services.Hosted;

public class DiscordStartupService : BackgroundService
{
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;
    private TaskCompletionSource<bool> _taskCompletionSource;
    private int _shardsReady;

    public DiscordStartupService(
        DiscordShardedClient discordShardedClient,
        IOptions<DiscordBotOptions> discordBotOptions)
    {
        _discordShardedClient = discordShardedClient;
        _discordBotOptions = discordBotOptions;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PrepareClientAwaiter();
        await _discordShardedClient.LoginAsync(TokenType.Bot, _discordBotOptions.Value.Token);
        await _discordShardedClient.StartAsync();
        await WaitForReadyAsync(stoppingToken);
    }

    private void PrepareClientAwaiter()
    {
        _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _shardsReady = 0;

        _discordShardedClient.ShardReady += OnShardReady;
    }
    
    private Task OnShardReady(DiscordSocketClient _)
    {
        _shardsReady++;
        if (_shardsReady == _discordShardedClient.Shards.Count)
        {
            _taskCompletionSource!.TrySetResult(true);
            _discordShardedClient.ShardReady -= OnShardReady;
        }
        return Task.CompletedTask;
    }

    private Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (_taskCompletionSource is null)
            throw new InvalidOperationException("The sharded client has not been registered correctly. Did you use ConfigureDiscordShardedHost on your HostBuilder?");

        if (_taskCompletionSource.Task.IsCompleted)
            return _taskCompletionSource.Task;

        var registration = cancellationToken.Register(
            state => { ((TaskCompletionSource<object>)state!).TrySetResult(null!); },
            _taskCompletionSource);

        return _taskCompletionSource.Task.ContinueWith(_ => registration.DisposeAsync(), cancellationToken);
    }
}