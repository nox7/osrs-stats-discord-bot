using AngleSharp.Common;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DagothUrDiscordBot.StatFetcher
{
    internal class StatFetcher
    {

        const string GROUP_IRONMAN_BASE_URL = "https://secure.runescape.com/m=hiscore_oldschool_ironman/group-ironman/view-group";

        private string groupName;

        public StatFetcher(string groupName) {
            this.groupName = groupName;
        }

        public async Task<string> GetGroupStats()
        {
            string hiscoresHTML = await GetGroupIronManHiscoreHTML(this.groupName);
            List<Player> players = ParsePlayersAndStatsFromHiScoresResponseBody(hiscoresHTML);
            string discordMessageToReturn = "";
            discordMessageToReturn += new string('=', 30) + "\n";
            discordMessageToReturn += $"===== Stats for **{this.groupName}** =====\n";
            discordMessageToReturn += new string('=', 30) + "\n\n";

            foreach (Player player in players)
            {
                discordMessageToReturn += new string('-', 25) + "\n";
                discordMessageToReturn += player.ToStringForDiscordWithoutSkills() + "\n";
            }

            return discordMessageToReturn;
        }

        private async Task<string> GetGroupIronManHiscoreHTML(string groupName)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            HttpClient client = new HttpClient(handler)
            {
                BaseAddress = new Uri(GROUP_IRONMAN_BASE_URL)
            };

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"?name={Uri.EscapeDataString(groupName)}");

            using HttpResponseMessage response = await client.SendAsync( request );
            // response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private List<Player> ParsePlayersAndStatsFromHiScoresResponseBody(string responseBody)
        {
            HtmlParser parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument( responseBody );
            IHtmlCollection<IElement> playerRows = document.QuerySelectorAll(".uc-scroll__table-row--type-player");
            List<Player> players = new List<Player>();

            foreach (IElement element in playerRows)
            {
                // Will have 3 direct children
                var innerCells = element.QuerySelectorAll(".uc-scroll__table-cell");
                IElement? nameOuterCell = innerCells.GetItemByIndex(0);
                IElement? totalLevelCell = innerCells.GetItemByIndex(1);
                IElement? totalXPCell = innerCells.GetItemByIndex(2);

                IElement? nameElement = nameOuterCell.QuerySelector(".uc-scroll__link")!;

                string totalLevelWithCommas = totalLevelCell.InnerHtml;
                string totalXPWithCommas = totalXPCell.InnerHtml;

                int totalLevel = Int32.Parse(totalLevelWithCommas.Replace(",", ""));
                int totalXP = Int32.Parse(totalXPWithCommas.Replace(",", ""));

                string playerName = nameElement.InnerHtml;

                // playerName could have "&nbsp;" in it. Replace this with an actual space
                playerName = playerName.Replace("&nbsp;", " ");

                // Inside the "nameOuterCell" will exist a button with class "ua-expand" that has an attribute named "data-js-expand-memberid"
                // This data-js-expand-memberid attribute holds a massive integer that represents <tr> elements that have the attribute 
                // data-js-skill-row-memberid whose value is equal to data-js-expand-memberid.
                // Those <tr>s with those attributes represent the skills for that player (memberid).

                // Get the "Expand" button
                IElement? expandButton = nameOuterCell.QuerySelector(".ua-expand")!;

                // Fetch the value of data-js-expand-memberid, but leave it as a string and do not parse as an Int64
                string memberID = expandButton.GetAttribute("data-js-expand-memberid")!;

                // Fetch all of the skill rows
                var skillTableRowElements = document.QuerySelectorAll(".uc-scroll__table-row.uc-scroll__table-row--type-skill");
                List<PlayerSkill> skills = new List<PlayerSkill>();

                foreach (IElement skillTableRow in skillTableRowElements)
                {
                    // Check that the attribute data-js-skill-row-memberid value is equal to memberID
                    string skillRowMemberID = skillTableRow.GetAttribute("data-js-skill-row-memberid")!;
                    if (skillRowMemberID == memberID)
                    {
                        // This is a skill for that player
                        // Fetch The .uc-scroll__table-cell elements
                        var skillColumns = skillTableRow.QuerySelectorAll(".uc-scroll__table-cell");

                        // 0 = Skill name element
                        // 1 = Skill level element
                        // 2 = Skill XP element
                        IElement skillNameElement = skillColumns.GetItemByIndex(0);
                        IElement skillLevelElement = skillColumns.GetItemByIndex(1);
                        IElement skillXPElement = skillColumns.GetItemByIndex(2);

                        string skillName = skillNameElement.TextContent;
                        int skillLevel = Int32.Parse(skillLevelElement.TextContent.Replace(",", ""));
                        int skillXP = Int32.Parse(skillXPElement.TextContent.Replace(",", ""));
                        PlayerSkill playerSkill = new PlayerSkill(skillName, skillLevel, skillXP);
                        skills.Add(playerSkill);
                    }
                }

                // Create the player object
                Player player = new Player(playerName, totalLevel, totalXP, skills);
                players.Add(player);
            }

            // Sort by highest total level
            players = players.OrderByDescending(player => player.GetTotalLevel()).ToList();

            return players;
        }
    }
}
