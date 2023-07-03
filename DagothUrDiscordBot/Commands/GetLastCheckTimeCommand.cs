using Discord;
using System.Diagnostics;

namespace DagothUrDiscordBot.Commands
{
    internal class GetLastCheckTimeCommand
    {
        public Embed GetEmbedForLastCheckTimeAndNextCheckTime()
        {
            var embed = new EmbedBuilder()
            {
                Title = $"Player Check Timer Stats",
                Description = "",
                Color = new Color(0, 0, 255)
            };

            DateTime currentTime = DateTime.Now;
            DateTime lastTick = DagothUr.GetInstance().GetLastTimerTickDateTime();
            DateTime nextTick = lastTick.AddMilliseconds(DagothUr.GetInstance().GetSkillCheckTimerIntervalInMs());

            TimeSpan timeSinceLastTick = currentTime - lastTick;
            TimeSpan timeSpanToNextTick = nextTick - currentTime;

            double secondsSinceLastTick = timeSinceLastTick.TotalSeconds;
            double secondsToNextTick = timeSpanToNextTick.TotalSeconds;

            embed.AddField("Last check", $"{GetTimeDifferenceAsHumanTimeString(secondsSinceLastTick)} ago");
            embed.AddField("Next check", $"In {GetTimeDifferenceAsHumanTimeString(secondsToNextTick)}");

            return embed.Build();
        }

        private string GetTimeDifferenceAsHumanTimeString(double secondsDifference)
        {
            if (secondsDifference < 60)
            {
                return $"{(secondsDifference):n0} seconds";
            }
            else if (secondsDifference < 3600 - 1)
            {
                return $"{(secondsDifference / 60):n0} minutes";
            }
            else
            {
                return $"{(secondsDifference / (3600)):n0} hours";
            }
        }
    }
}
