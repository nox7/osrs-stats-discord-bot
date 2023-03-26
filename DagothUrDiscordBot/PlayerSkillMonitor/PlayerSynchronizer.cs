using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.StatFetcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.PlayerSkillMonitor
{
    internal class PlayerSynchronizer
    {

        public PlayerSynchronizer() { }

        public async Task<List<HighscoresPlayer>> GetGIMHighscorePlayers(string gimName)
        {
            StatFetcher.StatFetcher statFetcher = new(gimName);
            string highscoreHTML = await statFetcher.GetGroupIronManHiscoreHTML();
            return statFetcher.ParsePlayersAndStatsFromHiScoresResponseBody(highscoreHTML);
        }

        public async Task SynchronizePlayersIntoDatabase(string gimName)
        {
            List<HighscoresPlayer> players = await GetGIMHighscorePlayers(gimName);

            foreach ( HighscoresPlayer hsPlayer in players )
            {
                // Are they in the database?
                Player? player = GetPlayerFromDatabase(hsPlayer);

                if (player == null)
                {
                    // Create this player and their skills into the database
                    Console.WriteLine($"{hsPlayer.GetName()} is not in the database.");
                    CreatePlayerAndSkillsInDatabase(hsPlayer);
                }
                else
                {
                    Console.WriteLine($"{hsPlayer.GetName()} exists in DB. Checking if they have made any skill gains.");
                    List<ChangedSkill> changedSkills = GetPlayerChangedSkills(hsPlayer);
                    if (changedSkills.Count > 0)
                    {
                        Console.WriteLine($"{hsPlayer.GetName()} Has made gains!");
                        foreach (ChangedSkill changedSkill in changedSkills)
                        {
                            Console.WriteLine($"{changedSkill.Name} by {changedSkill.DeltaLevel}");
                        }

                        // Update the skills in the DB to be current with the HighscorePlayer
                        UpdatePlayerSkillsInDatabase(player.Id, hsPlayer);
                    }
                    else
                    {
                        Console.WriteLine($"{hsPlayer.GetName()} Has made no gains since the last check.");
                    }
                }
            }
        }

        /**
         * Checks if the provided HighscorePlayer's skills (which are most current)
         * have any differences with the saved PlayerSkills (in the database).
         * 
         * If any are different, returns a list of the changed skills
         */
        public List<ChangedSkill> GetPlayerChangedSkills(HighscoresPlayer hsPlayer)
        {
            // Get the player from the dstabase
            Player? databasePlayer = GetPlayerFromDatabase(hsPlayer);
            List<ChangedSkill> changedSkills = new();

            if (databasePlayer != null)
            {
                // Fetch the player's skills
                using (DagothUrContext database = new DagothUrContext())
                {
                    List<PlayerSkill> playerSkills = database.PlayerSkills.Where(
                        playerSkill => playerSkill.PlayerId == databasePlayer.Id
                    ).ToList<PlayerSkill>();

                    foreach (PlayerSkill dbPlayerSkill in playerSkills)
                    {
                        HighscoresPlayerSkill hsSkill = hsPlayer.GetSkillByName(dbPlayerSkill.SkillName)!;
                        if (dbPlayerSkill.SkillLevel != hsSkill.GetLevel())
                        {
                            // This skill is different
                            int changeDifference = hsSkill.GetLevel() - dbPlayerSkill.SkillLevel;
                            ChangedSkill changedSkill = new ChangedSkill(dbPlayerSkill.SkillName, changeDifference, hsSkill.GetLevel());

                            changedSkills.Add(changedSkill);
                        }
                    }
                }
            }

            return changedSkills;
        }

        public void UpdatePlayerSkillsInDatabase(int playerID, HighscoresPlayer hsPlayer)
        {
            using (DagothUrContext database = new DagothUrContext())
            {
                // Fetch the existing skills and update them with the hsPlayer's current stats
                foreach (HighscoresPlayerSkill hsSkill in hsPlayer.GetSkillList())
                {
                    PlayerSkill? existingSkill = database.PlayerSkills.Where(
                        playerSkill => playerSkill.SkillName == hsSkill.GetName()
                    ).FirstOrDefault();

                    if (existingSkill != null)
                    {
                        // Update it
                        existingSkill.SkillXp = hsSkill.GetXP();
                        existingSkill.SkillLevel = hsSkill.GetLevel();
                    }
                    else
                    {
                        // Create it
                        AddSkillToDatabase(playerID, hsSkill);
                    }
                }

                database.SaveChanges(true);
            }
        }

        public Player? GetPlayerFromDatabase(HighscoresPlayer hsPlayer)
        {
            // Find the player in the database
            using (DagothUrContext database = new DagothUrContext())
            {
                return database.Players.Where(
                    player => player.PlayerName == hsPlayer.GetName()
                ).FirstOrDefault();
            }
        }

        private void CreatePlayerAndSkillsInDatabase(HighscoresPlayer hsPlayer)
        {
            Player newDatabasePlayer = new Player();
            newDatabasePlayer.PlayerName = hsPlayer.GetName();
            newDatabasePlayer.LastDataFetchTimestamp = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            using (DagothUrContext database = new DagothUrContext())
            {
                database.Players.Add(newDatabasePlayer);

                // Save the changes so newDatabasePlayer has a populated Id
                database.SaveChanges(true);

                int playerID = newDatabasePlayer.Id;

                // Add all skills to database
                foreach (HighscoresPlayerSkill hsSkill in hsPlayer.GetSkillList())
                {
                    AddSkillToDatabase(playerID, hsSkill);
                }

                database.SaveChanges(true);
            }
        }

        private PlayerSkill AddSkillToDatabase(int playerID, HighscoresPlayerSkill hsPlayerSkill)
        {
            PlayerSkill newDatabaseSkill = new PlayerSkill();
            newDatabaseSkill.PlayerId = playerID;
            newDatabaseSkill.SkillXp = hsPlayerSkill.GetXP();
            newDatabaseSkill.SkillLevel = hsPlayerSkill.GetLevel();
            newDatabaseSkill.SkillName = hsPlayerSkill.GetName();

            using (DagothUrContext database = new DagothUrContext())
            {
                database.Add(newDatabaseSkill);
            }

            return newDatabaseSkill;
        }
    }
}
