using DiscordMarsSim;
using DiscordMarsSim.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;

internal class Program
{
    static readonly Queue<MarsMessage> messageQueue = new();
    static readonly Astrodynamics kinematics = new();

    static async Task Main()
    {
        FileInfo tokenFile;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
             tokenFile = new($"{Assembly.GetExecutingAssembly().Location.Replace("/DiscordMarsSim.dll", "")}/token.txt");
        } 
        else
        {
            tokenFile = new($"{Assembly.GetExecutingAssembly().Location.Replace("\\DiscordMarsSim.dll", "")}\\token.txt");
        }
        DiscordClient discord = new(new DiscordConfiguration()
        {
            Token = File.ReadAllLines(tokenFile.FullName)[0],
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.MessageContents |
                        DiscordIntents.GuildMessages |
                        DiscordIntents.AllUnprivileged,
            MinimumLogLevel = LogLevel.Information
        });
        var services = new ServiceCollection()
            .AddSingleton<Astrodynamics>()
            .BuildServiceProvider();
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = [";msignore "],
            Services = services
        });
        commands.RegisterCommands<OrbitModule>();
        await discord.ConnectAsync();
        DiscordChannel marsChannel = await discord.GetChannelAsync(1199121819953799178);
        DiscordChannel testChannel = await discord.GetChannelAsync(380515333968232456);
        Console.WriteLine(kinematics.GetEntryCount());
        SetupEvents(discord, marsChannel);
        SetupEvents(discord, testChannel);
        SetupWebhook(marsChannel);
        SetupWebhook(testChannel);
        await Task.Delay(-1);
    }

    private static async void SetupWebhook(DiscordChannel channel)
    {
        var webhooks = await channel.GetWebhooksAsync();
        var validHooks = webhooks.Where(hook => hook.Name.Contains("Mars Sim"));
        if (!validHooks.Any())
        {
            await channel.CreateWebhookAsync("Mars Sim");
        }
    }

    static void SetupEvents(DiscordClient discord, DiscordChannel channel)
    {
        discord.MessageCreated += async (client, args) =>
        {
            if (args.Channel == channel)
            {
                if (args.Message.Content.Contains(";msignore") || args.Message.Author.IsBot)
                {
                    return;
                }
                messageQueue.Enqueue(new MarsMessage(args.Message,
                        Task.Delay(kinematics.GetTimeDelay())
                            .ContinueWith(
                                (task) =>
                                {
                                    SendMessage(channel, messageQueue.Dequeue());
                                }
                )));
                await args.Channel.DeleteMessageAsync(args.Message);
                discord.Logger.LogInformation($"Message queued from {args.Message.Author.Username}:\n    \"{args.Message.Content}\"");
                
            }
        };

    }

    async static void SendMessage(DiscordChannel channel, MarsMessage message)
    {
        // get webhook 
        var hooks = await channel.GetWebhooksAsync();
        var hook = hooks.Where(hook => hook.Name.Contains("Mars Sim")).FirstOrDefault();
        // get message
        var messageBuilder = new DiscordMessageBuilder(message.Message);
        if (hook is default(DiscordWebhook))
        {
            return;
        }
        var messageAuthor = (DiscordMember)message.Message.Author;
        // create message to send
        DiscordWebhookBuilder hookBuilder = new(messageBuilder)
        {
            AvatarUrl = messageAuthor.GetGuildAvatarUrl(ImageFormat.Auto),
            Username = messageAuthor.DisplayName
        };

        //await builder.SendAsync(channel);
        await hook.ExecuteAsync(hookBuilder);
    }

    record MarsMessage(DiscordMessage Message, Task DequeueCallback);
}