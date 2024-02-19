using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;

internal class Program
{
    static Queue<MarsMessage> messageQueue = new(); 
    static async Task Main(string[] args)
    {
        var tokenFile = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).GetFiles().Where(file => file.Name.Contains("token")).First();
        DiscordClient discord = new DiscordClient(new DiscordConfiguration() {
            // https://discord.com/api/oauth2/authorize?client_id=388080083807633408&permissions=536947712&scope=bot
            Token = File.ReadAllText(tokenFile.FullName),
            TokenType = TokenType.Bot,
            Intents =   DiscordIntents.MessageContents | 
                        DiscordIntents.GuildMessages |
                        DiscordIntents.AllUnprivileged
        });
        await discord.ConnectAsync();
        DiscordChannel marsChannel = await discord.GetChannelAsync(380515333968232456);
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
                        Task.Delay(new TimeSpan(0, 0, 10)).ContinueWith(
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