using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMarsSim.Commands
{
    internal class OrbitModule : BaseCommandModule
    {
        public Kinematics Kinematics { private get; set; }
        
        [Command("delay")]
        public async Task Delay(CommandContext context) => await context.RespondAsync($"The current one-way time delay is: {Kinematics.GetTimeDelay()}");
    }
}
