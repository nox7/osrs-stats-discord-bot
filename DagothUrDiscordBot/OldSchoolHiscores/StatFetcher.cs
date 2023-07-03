using AngleSharp.Common;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Diagnostics;

namespace DagothUrDiscordBot.OldschoolHiscores
{
    class StatFetcher
    {

        const string HISCORE_PERSONAL_POST_ENDPOINT = "https://secure.runescape.com/m=hiscore_oldschool/hiscorepersonal";

        public async Task<HiscoresPlayer?> GetHiscoresPlayerFromRSN(string rsn)
        {
            try
            {
                string hiscoresHTML = await GetHiscoresHTMLResponseForRSNLookup(rsn);
                return ParsePlayerAndSkillsFromHTML(rsn, hiscoresHTML);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private async Task<string> GetHiscoresHTMLResponseForRSNLookup(string rsn)
        {
            using HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            using HttpClient client = new HttpClient(handler)
            {
                BaseAddress = new Uri(HISCORE_PERSONAL_POST_ENDPOINT)
            };

            var payload = new Dictionary<string, string>();
            payload.Add("user1", rsn);
            payload.Add("submit", "Search");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new FormUrlEncodedContent(payload)
            };

            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private HiscoresPlayer ParsePlayerAndSkillsFromHTML(string rsn, string html)
        {
            HiscoresPlayer hiscorePlayer = new HiscoresPlayer(rsn);
            HtmlParser parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument(html);
            IElement? mainStatsContainer = document.GetElementById("contentHiscores");

            if (mainStatsContainer != null)
            {
                IElement statsTable = mainStatsContainer.QuerySelector("table")!;
                IHtmlCollection<IElement> tableRows = statsTable.QuerySelectorAll("tbody > tr");
                
                int counter = 0;
                foreach(IElement tableRow in tableRows)
                {
                    IHtmlCollection<IElement> tableColumns = tableRow.QuerySelectorAll("td");

                    if (tableColumns.Count() == 0)
                    {
                        Debug.WriteLine("Out of table rows with columns. Must be the end of the skills list.");
                        break;
                    }

                    // Skip the first four <tr> - they're just title / headings
                    if (counter < 3)
                    {
                        counter++;
                        continue;
                    }

                    // Index 3 is the total level and total XP row
                    if (counter == 3)
                    {
                        // [3] Fourth is total level
                        // [4] Fifth is total XP
                        IElement? totalLevelCell = tableColumns.GetItemByIndex(3);
                        IElement? totalXPCell = tableColumns.GetItemByIndex(4);
                        int totalLevel = Int32.Parse(totalLevelCell.TextContent.Trim().Replace(",", ""));
                        int totalXP = Int32.Parse(totalXPCell.TextContent.Trim().Replace(",", ""));
                        hiscorePlayer.SetTotalLevel(totalLevel);
                        hiscorePlayer.SetTotalXP(totalXP);
                        counter++;
                        continue;
                    }

                    
                    // [0] First is the skill icon
                    // [1] Second is the skill name as an anchor <a> inside the <td>
                    // [2] Third is the rank
                    // [3] Fourth is the skill level
                    // [4] Fifth is the XP in the skill
                    IElement? skillNameCell = tableColumns.GetItemByIndex(1);
                    IElement? skillLevelCell = tableColumns.GetItemByIndex(3);
                    IElement? skillXPCell = tableColumns.GetItemByIndex(4);

                    if (skillNameCell != null && skillLevelCell != null && skillXPCell != null)
                    {
                        string skillName = skillNameCell.TextContent.Trim();
                        int skillLevel = Int32.Parse(skillLevelCell.TextContent);
                        int skillXP = Int32.Parse(skillXPCell.TextContent.Replace(",", ""));

                        var newHiscorePlayerSkill = new HiscoresPlayerSkill(skillName, skillLevel, skillXP);
                        hiscorePlayer.AddSkill(newHiscorePlayerSkill);
                    }

                    counter++;
                }
            }

            return hiscorePlayer;
        }
    }
}
