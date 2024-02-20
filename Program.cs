using System.Reflection;
using DiscordMarsSim;
using DiscordMarsSim.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    static Queue<MarsMessage> messageQueue = new();
    static Kinematics kinematics = new();
    
    static async Task Main(string[] args)
    {
        FileInfo tokenFile = new($"{Assembly.GetExecutingAssembly().Location.Replace("\\DiscordMarsSim.dll", "")}\\token.txt");
        DiscordClient discord = new(new DiscordConfiguration() {
            Token = File.ReadAllLines(tokenFile.FullName)[0],
            TokenType = TokenType.Bot,
            Intents =   DiscordIntents.MessageContents | 
                        DiscordIntents.GuildMessages |
                        DiscordIntents.AllUnprivileged
        });
        var services = new ServiceCollection()
            .AddSingleton<Kinematics>()
            .BuildServiceProvider();
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = [";msignore "],
            Services = services
        });
        commands.RegisterCommands<OrbitModule>();
        await discord.ConnectAsync();
        DiscordChannel marsChannel = await discord.GetChannelAsync(380515333968232456);
        Console.WriteLine(kinematics.GetEntryCount());
        SetupEvents(discord, marsChannel);
        SetupWebhook(discord, marsChannel);
        await Task.Delay(-1);
    }

    private static async void SetupWebhook(DiscordClient discord, DiscordChannel channel)
    {
        var webhooks = await channel.GetWebhooksAsync();
        var validHooks = webhooks.Where(hook => hook.Name.Contains("Mars Sim"));
        if (validHooks.Count() == 0)
        {
            await channel.CreateWebhookAsync("Mars Sim");
        }
    }

    static void SetupEvents(DiscordClient discord, DiscordChannel marsChannel)
    {
        discord.MessageCreated += async (client, args) =>
        {
            if (args.Channel == marsChannel)
            {
                if (args.Message.Content.Contains(";msignore") || args.Message.Author.IsBot)
                {
                    return;
                }
                messageQueue.Enqueue(new MarsMessage(args.Message,
                        Task.Delay(kinematics.GetTimeDelay())
                            .ContinueWith(
                                (task) => {
                                    SendMessage(marsChannel, messageQueue.Dequeue());
                                }
                )));
                await args.Channel.DeleteMessageAsync(args.Message);
            }
        };
    }

    async static void SendMessage(DiscordChannel channel, MarsMessage message) {
        // get webhook 
        var hooks = await channel.GetWebhooksAsync();
        var hook = hooks.Where(hook => hook.Name.Contains("Mars Sim")).FirstOrDefault();
        // get message
        var messageBuilder = new DiscordMessageBuilder(message.message);
        if (hook is default(DiscordWebhook))
        {
            return;
        }
        var messageAuthor = (DiscordMember)message.message.Author;
        // create message to send
        DiscordWebhookBuilder hookBuilder = new DiscordWebhookBuilder(messageBuilder)
        {
            AvatarUrl = messageAuthor.GetGuildAvatarUrl(ImageFormat.Auto),
            Username = messageAuthor.DisplayName 
        };
        
        //await builder.SendAsync(channel);
        await hook.ExecuteAsync(hookBuilder);
    }

    record MarsMessage(DiscordMessage message, Task dequeueCallback);
}