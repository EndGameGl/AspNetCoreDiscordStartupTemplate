using System.Reflection;
using System.Threading.Tasks;
using AspNetCoreDiscordStartupTemplate.Options;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace AspNetCoreDiscordStartupTemplate.Services.Hosted;

public class DiscordStartupService : BackgroundService
{
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;
    private readonly InteractionService _interactionService;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;
    private TaskCompletionSource<bool> _taskCompletionSource;
    private int _shardsReady;

    public DiscordStartupService(
        DiscordShardedClient discordShardedClient,
        IOptions<DiscordBotOptions> discordBotOptions,
        InteractionService interactionService,
        CommandService commandService,
        IServiceProvider serviceProvider)
    {
        _discordShardedClient = discordShardedClient;
        _discordBotOptions = discordBotOptions;
        _interactionService = interactionService;
        _commandService = commandService;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordShardedClient.InteractionCreated += OnDiscordInteractionCreated;
        _discordShardedClient.MessageReceived += OnDiscordMessageReceived;
        PrepareClientAwaiter();
        await _discordShardedClient.LoginAsync(TokenType.Bot, _discordBotOptions.Value.Token);
        await _discordShardedClient.StartAsync();
        await WaitForReadyAsync(stoppingToken);

        if (stoppingToken.IsCancellationRequested || _taskCompletionSource.Task.Result is false)
            return;

        // load text commands
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        // load interactions
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        
        // register your commands here
        //await _interactionService.RegisterCommandsToGuildAsync();
    }
    
    private async Task OnDiscordInteractionCreated(SocketInteraction socketInteraction)
    {
        var shardedInteractionContext = new ShardedInteractionContext(_discordShardedClient, socketInteraction);
        await _interactionService.ExecuteCommandAsync(shardedInteractionContext, _serviceProvider);
    }
    
    private async Task OnDiscordMessageReceived(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage socketUserMessage)
            return;

        var argPos = 0;
        if (socketUserMessage.HasCharPrefix('!', ref argPos))
            return;
        if (socketUserMessage.Author.IsBot)
            return;

        var context = new ShardedCommandContext(_discordShardedClient, socketUserMessage);
        await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
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
            state => { ((TaskCompletionSource<bool>)state!).TrySetResult(false!); },
            _taskCompletionSource);

        return _taskCompletionSource.Task.ContinueWith(_ => registration.DisposeAsync(), cancellationToken);
    }
}