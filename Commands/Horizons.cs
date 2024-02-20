﻿using DSharpPlus.CommandsNext;
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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Kinematics Kinematics { private get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Command("delay")]
        public async Task Delay(CommandContext context) => await context.RespondAsync($"The current one-way time delay is: {Kinematics.GetTimeDelay()}");
    }
}
