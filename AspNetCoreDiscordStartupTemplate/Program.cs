using AspNetCoreDiscordStartupTemplate.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDiscord(client =>
        {
            // configure your client here
        },
        builder.Configuration);

var app = builder.Build();
await app.RunAsync();