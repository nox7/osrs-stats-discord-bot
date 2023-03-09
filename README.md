# OSRS Group Ironman Stats Discord Bot
Discord bot written in C# (Using .NET 6 and Discord.NET frameworks) that utilizes an HttpClient to fetch a given group ironman's member statistics by parsing the response body into an HTML DOM with AngleSharp and serializing the players and their stats into objects.

Can be versatile in usage for pure statistical output or tracking with in-memory (for long-running continuous applications) or stored in an SQL database for tracking over time over non-persistent application runs.

## Deployment
Primarily, we would deploy this to an Azure Web Service as a continuous application via WebJobs. However, you can also just spin up any server running .NET v6 and start the application by CLI without the use of Azure services.
