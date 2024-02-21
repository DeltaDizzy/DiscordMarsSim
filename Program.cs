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
    private static bool testing = false;

    static async Task Main()
    {
        FileInfo tokenFile;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            testing = false;
            tokenFile = new($"{Assembly.GetExecutingAssembly().Location.Replace("/DiscordMarsSim.dll", "")}/token.txt");
        }
        else
        {
            tokenFile = new($"{Assembly.GetExecutingAssembly().Location.Replace("\\DiscordMarsSim.dll", "")}\\token.txt");
        }
        string token = File.ReadAllLines(tokenFile.FullName)[testing ? 1 : 0];
        DiscordClient discord = new(new DiscordConfiguration()
        {
            Token = token,
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
        IEnumerable<DiscordChannel> allowedChannels = [
            await discord.GetChannelAsync(1209668565293072425),// test beta channel
            await discord.GetChannelAsync(380515333968232456), // test main channel
            await discord.GetChannelAsync(1199121819953799178) // spaceflight takes
        ];
        Action<DiscordChannel> initializer = (channel) =>
        {
            SetupEvents(discord, channel);
            SetupWebhook(channel);
        };
        foreach (DiscordChannel channel in allowedChannels)
        {
            initializer(channel);
        }
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
                //// save file
                //var attachments = args.Message.Attachments;
                //List<string> attachmentUrls = [];
                List<string> imageNames = [];
                //if (attachments.Count != 0)
                //{
                //    for (int i = 0; i < attachments.Count; i++)
                //    {
                //        imageNames.Add(attachments[i].FileName!);
                //        attachmentUrls.Add(attachments[i].Url);
                //    }
                //    await DownloadImages([.. attachmentUrls]);
                //}

                // algorithm
                // 1. get list of attachment urls
                // 2. attach guids to names and save the image names to message
                // 3. save imaes to disk
                // 4. at dequeue time, load images from disk based on stored names
                // 5. strip out GUIDs and add to webhook
                messageQueue.Enqueue(new MarsMessage(args.Message,
                        Task.Delay(testing ? new TimeSpan(0, 1, 0) : kinematics.GetTimeDelay())
                            .ContinueWith(
                                (task) =>
                                {
                                    SendMessage(messageQueue.Dequeue());
                                }
                ), imageNames));

                await args.Channel.DeleteMessageAsync(args.Message);
                discord.Logger.LogInformation($"Message queued from {args.Message.Author.Username}:\n    \"{args.Message.Content}\"");
            }
        };

    }

    async static void SendMessage(MarsMessage message)
    {
        var channel = message.Message.Channel;
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
            Username = messageAuthor.DisplayName,
        };
        // load images
        //if (message.ImageNames.Any())
        //{
        //    foreach (string item in message.ImageNames)
        //    {
        //        var image = LoadImage(item);
        //        hookBuilder.AddFile(image);
        //    }
        //}
        //await builder.SendAsync(channel);
        try
        {
            await hook.ExecuteAsync(hookBuilder);
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine(ae.Message);
            Console.WriteLine(ae.StackTrace);
            Console.WriteLine(ae.ParamName);
            Console.WriteLine("========================");
            Console.WriteLine($"Message Content: {message.Message.Content}");
        }
    }

    record MarsMessage(DiscordMessage Message, Task DequeueCallback, List<string> ImageNames);
}