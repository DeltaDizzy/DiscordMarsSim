using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DiscordMarsSim.Commands
{
    internal class OrbitModule : BaseCommandModule
    {
        public Astrodynamics Kinematics { private get; set; }

        [Command("delay")]
        public async Task Delay(CommandContext context) => await context.RespondAsync($"The current one-way time delay is: {Kinematics.GetTimeDelay()}");
    }
}
