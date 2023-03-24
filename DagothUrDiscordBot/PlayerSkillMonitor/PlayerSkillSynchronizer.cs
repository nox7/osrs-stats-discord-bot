using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.StatFetcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.PlayerSkillMonitor
{
    internal class PlayerSkillSynchronizer
    {
        private DagothUrContext database;

        public PlayerSkillSynchronizer(DagothUrContext database) {
            this.database = database;
        }

        public async Task SynchronizePlayersIntoDatabase()
        {
            StatFetcher.StatFetcher statFetcher = new(
                Environment.GetEnvironmentVariable("gimName") ?? string.Empty
            );

            string highscoreHTML = await statFetcher.GetGroupIronManHiscoreHTML();
            List<DagothUrDiscordBot.StatFetcher.Player> players = statFetcher.ParsePlayersAndStatsFromHiScoresResponseBody(highscoreHTML);

            foreach ( var highscorePlayer in players )
            {
                string highscorePlayerName = highscorePlayer.GetName();
                DagothUrDiscordBot.Models.Player? dbPlayer = this.database.Players.Where(
                    player => player.PlayerName == highscorePlayerName
                ).FirstOrDefault();

                if (dbPlayer == null)
                {
                    Console.WriteLine($"{highscorePlayerName} is not in the database.");
                }
                else
                {
                    Console.WriteLine($"{highscorePlayerName} exists in DB.");
                }
            }
        }
    }
}
