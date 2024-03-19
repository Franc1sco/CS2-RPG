using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using Dapper;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API;
using static Dapper.SqlMapper;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("DatabaseType")]
    public string DatabaseType { get; set; } = "SQLite";
    [JsonPropertyName("DatabaseFilePath")]
    public string DatabaseFilePath { get; set; } = "/csgo/addons/counterstrikesharp/plugins/RPG/RPG-db.sqlite";
    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; }
    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "";
    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";
    [JsonPropertyName("DatabaseTable")]
    public string DatabaseTable { get; set; } = "rpgtable";
    [JsonPropertyName("Comment")]
    public string Comment { get; set; } = "Use SQLite or MySQL as Database Type.";
    [JsonPropertyName("XPLevel")] public int XPLevel { get; set; } = 2500;

    [JsonPropertyName("HPIncreasePerLevel")] public int HPIncreasePerLevel { get; set; } = 25;
    [JsonPropertyName("SpeedIncreasePerLevel")] public float SpeedIncreasePerLevel { get; set; } = 0.05f;

}

namespace RPG
{
    public class PlayerSkills
    {
        public int Skill1Points { get; set; }
        public int Skill2Points { get; set; }
        public int Skill3Points { get; set; }
        public int Skill4Points { get; set; }
        public int Skill5Points { get; set; }
        public int AvailablePoints { get; set; }

        public int GetSkillPointsByColumnName(string columnName)
        {
            switch (columnName)
            {
                case "skillone": return Skill1Points;
                case "skilltwo": return Skill2Points;
                case "skillthree": return Skill3Points;
                case "skillfour": return Skill4Points;
                case "skillfive": return Skill5Points;
                default: throw new ArgumentException("Invalid column name");
            }
        }

        public void IncrementSkillPointsByColumnName(string columnName)
        {
            switch (columnName)
            {
                case "skillone": Skill1Points++; break;
                case "skilltwo": Skill2Points++; break;
                case "skillthree": Skill3Points++; break;
                case "skillfour": Skill4Points++; break;
                case "skillfive": Skill5Points++; break;
                default: throw new ArgumentException("Invalid column name");
            }
        }
    }
    public class MySQLStorage
    {
        private string ip;
        private int port;
        private string user;
        private string password;
        private string database;
        private string table;
        private bool isSQLite;
        private int XPLevel;

        private MySqlConnection? conn;
        private SqliteConnection? connLocal;
        public MySQLStorage(string ip, int port, string user, string password, string database, string table, bool isSQLite, string sqliteFilePath, int xPLevel)
        {
            string connectStr = $"server={ip};port={port};user={user};password={password};database={database};";
            this.ip = ip;
            this.port = port;
            this.user = user;
            this.password = password;
            this.database = database;
            this.table = table;
            this.isSQLite = isSQLite;
            this.XPLevel = xPLevel;

            if (isSQLite)
            {
                string dbFilePath = Server.GameDirectory + sqliteFilePath;

                var connectionString = $"Data Source={dbFilePath};";

                connLocal = new SqliteConnection(connectionString);

                connLocal.Open();

                var query = $@"
                CREATE TABLE IF NOT EXISTS `{table}` (
                steamid INTEGER PRIMARY KEY,
                kills INTEGER NOT NULL DEFAULT 0,
                deaths INTEGER NOT NULL DEFAULT 0,
                survivals INTEGER NOT NULL DEFAULT 0,
                headshot INTEGER NOT NULL DEFAULT 0,
                knifekills INTEGER NOT NULL DEFAULT 0,
                grenadekills INTEGER NOT NULL DEFAULT 0,
                grenadeplants INTEGER NOT NULL DEFAULT 0,
                zombieinfections INTEGER NOT NULL DEFAULT 0,
                points INTEGER NOT NULL DEFAULT 0,
                pointscalc INTEGER NOT NULL DEFAULT 0,       
                skillavailable INTEGER NOT NULL DEFAULT 0,
                skillone INTEGER NOT NULL DEFAULT 0,
                skilltwo INTEGER NOT NULL DEFAULT 0,
                skillthree INTEGER NOT NULL DEFAULT 0,
                skillfour INTEGER NOT NULL DEFAULT 0,
                skillfive INTEGER NOT NULL DEFAULT 0,
                skillsix INTEGER NOT NULL DEFAULT 0,
                skillseven INTEGER NOT NULL DEFAULT 0,
                skilleight INTEGER NOT NULL DEFAULT 0,
                skillnine INTEGER NOT NULL DEFAULT 0,      
                level INTEGER NOT NULL DEFAULT 1,
                rank INTEGER NOT NULL DEFAULT 0,
                skin TEXT,
                class TEXT,
                zclass TEXT,
                hclass TEXT     
                );";

                using (SqliteCommand command = new SqliteCommand(query, connLocal))
                {
                    command.ExecuteNonQuery();
                }
                connLocal.Close();
            }
            else
            {
                conn = new MySqlConnection(connectStr);
                conn.Execute($@"
                CREATE TABLE IF NOT EXISTS `{table}` (
                    `steamid` varchar(64) NOT NULL PRIMARY KEY,
                    `kills` bigint NOT NULL DEFAULT 0,
                    `deaths` bigint NOT NULL DEFAULT 0,
                    `survivals` bigint NOT NULL DEFAULT 0,
                    `headshot` bigint NOT NULL DEFAULT 0,
                    `knifekills` bigint NOT NULL DEFAULT 0,
                    `grenadekills` bigint NOT NULL DEFAULT 0,
                    `grenadeplants` bigint NOT NULL DEFAULT 0,
                    `zombieinfections` bigint NOT NULL DEFAULT 0,
                    `points` bigint NOT NULL DEFAULT 0,
                    `pointscalc` bigint NOT NULL DEFAULT 0,       
                    `skillavailable` bigint NOT NULL DEFAULT 0,
                    `skillone` int NOT NULL DEFAULT 0,
                    `skilltwo` int NOT NULL DEFAULT 0,
                    `skillthree` int NOT NULL DEFAULT 0,
                    `skillfour` int NOT NULL DEFAULT 0,
                    `skillfive` int NOT NULL DEFAULT 0,
                    `skillsix` int NOT NULL DEFAULT 0,
                    `skillseven` int NOT NULL DEFAULT 0,
                    `skilleight` int NOT NULL DEFAULT 0,
                    `skillnine` int NOT NULL DEFAULT 0,      
                    `level` bigint NOT NULL DEFAULT 1,
                    `rank` bigint NOT NULL DEFAULT 0,
                    `skin` TEXT,
                    `class` TEXT,
                    `zclass` TEXT,
                    `hclass` TEXT        
                );");
            }
        }
        public async Task FirstTimeRegister(ulong SteamID)
        {
            if (isSQLite)
            {
                if (connLocal == null)
                {
                    Console.WriteLine("Error connection");
                    return;
                }
                await connLocal.OpenAsync();
                var exists = await connLocal.QueryFirstOrDefaultAsync($"SELECT steamid FROM {table} WHERE steamid = @SteamID", new { SteamID });
                Console.WriteLine("hecho1");
                if (exists == null)
                {
                    Console.WriteLine("hecho2");
                    var query = $@"
        INSERT OR IGNORE INTO `{table}` (`steamid`) VALUES (@SteamID);
        ";
                    var command = new SqliteCommand(query, connLocal);
                    command.Parameters.AddWithValue("@SteamID", SteamID);
                    await command.ExecuteNonQueryAsync();
                }
                Console.WriteLine("hecho3");
                connLocal.Close();
            }
            else
            {
                await conn!.OpenAsync();
                var exists = await conn.QueryFirstOrDefaultAsync($"SELECT `steamid` FROM `{table}` WHERE `steamid` = @SteamID", new { SteamID });

                if (exists == null)
                {
                    var sql = $@"
        INSERT INTO `{table}` (`steamid`) VALUES (@SteamID) ON DUPLICATE KEY UPDATE `steamid` = @SteamID;
        ";
                    await conn.ExecuteAsync(sql, new { SteamID });
                }
                conn.Close();
            }
        }

        public async Task UpdateDb(ulong steamID, string columnName, int value)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal.OpenAsync();
                    var sql = $@"UPDATE {table} SET {columnName} = {columnName} + @Value WHERE steamid = @SteamID;";
                    await connLocal.ExecuteAsync(sql, new { SteamID = steamID, Value = value });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in UpdateDb: {ex.Message}");
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"UPDATE `{table}` SET {columnName} = {columnName} + @Value WHERE `steamid` = @SteamID;";
                        await conn.ExecuteAsync(sql, new { SteamID = steamID, Value = value });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in UpdateDb: {ex.Message}");
                    }
                }
            }

        }
        public async Task SetDb(ulong steamID, string columnName, int value)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal.OpenAsync();
                    var sql = $@"UPDATE {table} SET {columnName} = @Value WHERE steamid = @SteamID;";
                    await connLocal.ExecuteAsync(sql, new { SteamID = steamID, Value = value });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in SetDb: {ex.Message}");
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"UPDATE `{table}` SET {columnName} = @Value WHERE `steamid` = @SteamID;";
                        await conn.ExecuteAsync(sql, new { SteamID = steamID, Value = value });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in SetDb: {ex.Message}");
                    }
                }
            }

        }
        public async Task UpdateTxtDb(ulong steamID, string columnName, string newValue)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal!.OpenAsync();
                    var sql = $@"UPDATE {table} SET {columnName} = @NewValue WHERE steamid = @SteamID;";
                    await connLocal.ExecuteAsync(sql, new { SteamID = steamID, NewValue = newValue });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in UpdateTxtDb: {ex.Message}");
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"UPDATE `{table}` SET {columnName} = @NewValue WHERE `steamid` = @SteamID;";
                        await conn.ExecuteAsync(sql, new { SteamID = steamID, NewValue = newValue });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in UpdateTxtDb: {ex.Message}");
                    }
                }
            }
        }

        public async Task<int> GetPlayerIntAttribute(ulong steamID, string columnName)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal!.OpenAsync();
                    var sql = $@"SELECT {columnName} FROM `{table}` WHERE steamid = @SteamID;";
                    var attributeValue = await connLocal.QueryFirstOrDefaultAsync<int?>(sql, new { SteamID = steamID });
                    return attributeValue ?? 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetPlayerIntAttribute: {ex.Message}");
                    return 0;
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"SELECT `{columnName}` FROM `{table}` WHERE `steamid` = @SteamID;";
                        var attributeValue = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { SteamID = steamID });
                        return attributeValue ?? 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in GetPlayerIntAttribute: {ex.Message}");
                        return 0;
                    }
                }
            }
        }
        public async Task<string> GetPlayerAttribute(ulong steamID, string columnName)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal!.OpenAsync();
                    var sql = $@"SELECT {columnName} FROM {table} WHERE steamid = @SteamID;";
                    var attributeValue = await connLocal.QueryFirstOrDefaultAsync<string>(sql, new { SteamID = steamID });
                    return attributeValue ?? string.Empty; // Return an empty string if null to avoid returning null
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetPlayerAttribute: {ex.Message}");
                    return string.Empty; // Return an empty string in case of an exception
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"SELECT `{columnName}` FROM `{table}` WHERE `steamid` = @SteamID;";
                        var attributeValue = await conn.QueryFirstOrDefaultAsync<string>(sql, new { SteamID = steamID });
                        return attributeValue ?? string.Empty; // Return an empty string if null to avoid returning null
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in GetPlayerAttribute: {ex.Message}");
                        return string.Empty; // Return an empty string in case of an exception
                    }
                }
            }
        }

        public async Task<PlayerSkills> LoadPlayerSkillsFromDatabase(ulong steamID)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal!.OpenAsync();
                    var sql = $@"SELECT skillone AS Skill1Points, skilltwo AS Skill2Points, skillthree AS Skill3Points, skillfour AS Skill4Points, skillfive AS Skill5Points, skillavailable AS AvailablePoints FROM {table} WHERE steamid = @SteamID;";
                    var playerSkills = await connLocal.QueryFirstOrDefaultAsync<PlayerSkills>(sql, new { SteamID = steamID });

                    return playerSkills ?? new PlayerSkills();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in LoadPlayerSkillsFromDatabase: {ex.Message}");
                    return new PlayerSkills(); // Return an empty object to avoid null reference exceptions
                }
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var sql = $@"SELECT skillone AS Skill1Points, skilltwo AS Skill2Points, skillthree AS Skill3Points, skillfour AS Skill4Points, skillfive AS Skill5Points, skillavailable AS AvailablePoints FROM {table} WHERE steamid = @SteamID;";
                        var playerSkills = await conn.QueryFirstOrDefaultAsync<PlayerSkills>(sql, new { SteamID = steamID });

                        return playerSkills ?? new PlayerSkills();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in LoadPlayerSkillsFromDatabase: {ex.Message}");
                        return new PlayerSkills(); // Return an empty object to avoid null reference exceptions
                    }
                }
            }
        }
        public async Task<bool> CheckUpdateRankUp(ulong steamID)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal!.OpenAsync();
                    var points = await connLocal.QueryFirstOrDefaultAsync<int>($@"SELECT pointscalc FROM {table} WHERE steamid = @SteamID;", new { SteamID = steamID });
                    if (points >= XPLevel)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CheckUpdateRankUp: {ex.Message}");
                }
                return false;
            }
            else
            {
                var connectionString = $"server={ip};port={port};user={user};password={password}:;database={database};";
                using (var conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        await conn.OpenAsync();
                        var points = await conn.QueryFirstOrDefaultAsync<int>($@"SELECT `pointscalc` FROM `{table}` WHERE `steamid` = @SteamID;", new { SteamID = steamID });
                        if (points >= XPLevel)
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in CheckUpdateRankUp: {ex.Message}");
                    }
                    return false;
                }
            }
        }

    }
    public class RPG : BasePlugin, IPluginConfig<ConfigGen>
    {
        public ConfigGen Config { get; set; } = null!;
        public void OnConfigParsed(ConfigGen config) { Config = config; }

        private MySQLStorage? storage;
        public override string ModuleName => "RPG";
        public override string ModuleVersion => "1.0 - 19/03/2024-b";
        public override string ModuleAuthor => "Franc1sco Franug";
        public override void Load(bool hotReload)
        {
            storage = new MySQLStorage(
                Config.DatabaseHost,
                Config.DatabasePort,
                Config.DatabaseUser,
                Config.DatabasePassword,
                Config.DatabaseName,
                Config.DatabaseTable,
                Config.DatabaseType == "SQLite",
                Config.DatabaseFilePath,
                Config.XPLevel
            );
            base.Load(hotReload);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurtMultiplier, HookMode.Pre);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJump, HookMode.Pre);
            RegisterEventHandler<EventPlayerSpawn>(eventPlayerSpawn);

            AddCommand("skills", "Opens menu to upgrade skills", OnSkillsCommand);
            AddCommand("showskills", "show skills in chat", OnShowskillsCommand);

            
            if (hotReload)
            {
                AddTimer(5.0f, TimerCheckVelocity, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            RegisterListener<Listeners.OnMapStart>(OnMapStartEvent);

        }
        private void OnMapStartEvent(string mapName)
        {
            AddTimer(5.0f, TimerCheckVelocity, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void TimerCheckVelocity()
        {
            List<CCSPlayerController> players = Utilities.GetPlayers();

            foreach (CCSPlayerController player in players)
            {
                if (player == null || !player.IsValid || player.IsHLTV || player.SteamID.ToString() == "" || !player.PawnIsAlive) continue;

                var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                if (speedPoints >= 1)
                {
                    SetPlayerSpeed(player.PlayerPawn.Value, speedPoints * Config.SpeedIncreasePerLevel);
                }
            }
        }
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && !@event.Headshot && @event.Weapon != "knife" && @event.Weapon != "hegrenade" && @event.Attacker != @event.Userid) // simple kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "kills", 1);
                storage?.UpdateDb(steamid, "points", 200);
                storage?.UpdateDb(steamid, "pointscalc", 200);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}200 xp {ChatColors.Gold}for a kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Userid.IsBot) // simple death
            {
                var steamid = @event.Userid.SteamID;
                storage?.UpdateDb(steamid, "deaths", 1);
            }
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && @event.Headshot && IsCT(@event.Attacker) && @event.Attacker != @event.Userid) // headshot kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "points", 250);
                storage?.UpdateDb(steamid, "pointscalc", 250);
                storage?.UpdateDb(steamid, "headshot", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}250 xp {ChatColors.Gold}for a headshot kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && @event.Weapon == "knife" && IsCT(@event.Attacker) && @event.Attacker != @event.Userid) // knife kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "points", 750);
                storage?.UpdateDb(steamid, "pointscalc", 750);
                storage?.UpdateDb(steamid, "knifekills", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}750 xp {ChatColors.Gold}for a knife kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && @event.Weapon == "hegrenade" && @event.Attacker != @event.Userid) // grenade kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "points", 300);
                storage?.UpdateDb(steamid, "pointscalc", 300);
                storage?.UpdateDb(steamid, "grenadekills", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}300 xp {ChatColors.Gold}for a grenade kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }

            return HookResult.Continue;
        }
        public void RankUpPlayerOrNo(CCSPlayerController player)
        {
            var steamid = player.SteamID;
            if (storage != null)
            {
                bool canRankUp = storage.CheckUpdateRankUp(steamid).GetAwaiter().GetResult();
                if (canRankUp)
                {
                    storage?.UpdateDb(steamid, "level", 1);
                    storage?.UpdateDb(steamid, "skillavailable", 1);
                    storage?.SetDb(steamid, "pointscalc", 0);
                    if (storage != null)
                    {
                        int level = storage.GetPlayerIntAttribute(steamid, "level").GetAwaiter().GetResult();
                        player.PrintToChat($" {ChatColors.Gold}You have leveled up! You are now {ChatColors.Lime}level {level}{ChatColors.Gold}.");
                        player.PrintToChat($" {ChatColors.Gold}You earned a skill point for levelling up! Type {ChatColors.Lime}!skills{ChatColors.Gold} to use it.");
                        BroadcastMessageToAllExcept(player, $" {ChatColors.Lime}{player.PlayerName} {ChatColors.Gold}have leveled up! He is now {ChatColors.Lime}level {level}{ChatColors.Gold}.");
                    }
                }
            }
        }
        public void BroadcastMessageToAllExcept(CCSPlayerController excludedPlayer, string message)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player != excludedPlayer && player.IsValid && !player.IsBot && player != null)
                {
                    player.PrintToChat(message);
                }
            }
        }
        public void GiveMoney(CCSPlayerController player, int amount)
        {
            if (player == null || !player.IsValid || player.IsBot || !player.IsValid)
            {
                return;
            }
            if (player.InGameMoneyServices != null && amount > 0)
            {
                player.InGameMoneyServices.Account += amount;
            }
        }

        ///// ################### SKILLS START ################### /////

        private Dictionary<ulong, PlayerSkills> playerSkillsCache = new Dictionary<ulong, PlayerSkills>();
        private Dictionary<ulong, PlayerSkills> playerSkillsDictionary = new Dictionary<ulong, PlayerSkills>();
        public async Task<PlayerSkills> LoadPlayerSkillsFromDatabase(ulong steamID)
        {
            if (storage != null)
            {
                return await storage.LoadPlayerSkillsFromDatabase(steamID);
            }
            else
            {
                return new PlayerSkills
                {
                    Skill1Points = 0,
                    Skill2Points = 0,
                    Skill3Points = 0,
                    Skill4Points = 0,
                    Skill5Points = 0,
                    AvailablePoints = 0
                };
            }
        }
        private void OnShowskillsCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot)
            {
                return; // Check if the player object is valid and not a bot
            }

            var steamid = player.SteamID;

            // Attempt to retrieve the player's skills from the cache
            if (playerSkillsCache.TryGetValue(steamid, out PlayerSkills? playerSkills))
            {
                // If found, print each skill and its points
                player.PrintToChat($"{ChatColors.Green}[Skills]{ChatColors.Gold} Your skills are:");
                player.PrintToChat($" {ChatColors.Gold}Health: {ChatColors.Lime}{playerSkills.Skill1Points}/10");
                player.PrintToChat($" {ChatColors.Gold}Speed: {ChatColors.Lime}{playerSkills.Skill2Points}/10");
                player.PrintToChat($" {ChatColors.Gold}Jump: {ChatColors.Lime}{playerSkills.Skill3Points}/10");
                player.PrintToChat($" {ChatColors.Gold}Knife Damage: {ChatColors.Lime}{playerSkills.Skill4Points}/10");
                player.PrintToChat($" {ChatColors.Gold}Grenade Damage: {ChatColors.Lime}{playerSkills.Skill5Points}/10");
                player.PrintToChat($" {ChatColors.Green}Available Skill Points: {ChatColors.Lime}{playerSkills.AvailablePoints}");
            }
            else
            {
                // If not found in cache, you might want to load from database or display an error
                // This is just an error message, implement loading from DB as needed
                player.PrintToChat($"{ChatColors.Red}Error: Could not retrieve your skills. Please try again.");
            }
        } // for debugging, to see if dictionary contains the right values
        private async void OnSkillsCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            var steamid = player.SteamID;
            if (storage != null)
            {
                int availablePoints = await storage.GetPlayerIntAttribute(steamid, "skillavailable");
                int healthPoints = await storage.GetPlayerIntAttribute(steamid, "skillone");
                int speedPoints = await storage.GetPlayerIntAttribute(steamid, "skilltwo");
                int jumpPoints = await storage.GetPlayerIntAttribute(steamid, "skillthree");
                int knifeDmgPoints = await storage.GetPlayerIntAttribute(steamid, "skillfour");
                int GrenaeDmgPoints = await storage.GetPlayerIntAttribute(steamid, "skillfive");

                Server.NextFrame(() =>
                {
                    CenterHtmlMenu menu = new CenterHtmlMenu($"Skills Menu");
                    menu.Title = $"<font color='lightblue'>Available Skill Points : <font color='pink'>{availablePoints}<br><font color='yellow'>Upgrade your Skills :<font color='white'><font color='white'>";
                    menu.AddMenuOption($"Health [{healthPoints}/10]", (p, option) => UpdateSkill(p, "Health", "skillone", commandInfo));
                    menu.AddMenuOption($"Speed [{speedPoints}/10]", (p, option) => UpdateSkill(p, "Speed", "skilltwo", commandInfo));
                    menu.AddMenuOption($"Jump [{jumpPoints}/10]", (p, option) => UpdateSkill(p, "Jump", "skillthree", commandInfo));
                    menu.AddMenuOption($"Knife Damage [{knifeDmgPoints}/10]", (p, option) => UpdateSkill(p, "Knife Damage", "skillfour", commandInfo));
                    menu.AddMenuOption($"Grenade Damage [{GrenaeDmgPoints}/10]", (p, option) => UpdateSkill(p, "Grenade Damage", "skillfive", commandInfo));
                    MenuManager.OpenCenterHtmlMenu(this, player, menu);
                });
            }
        }
        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (@event.Userid != null && @event.Userid.IsValid)
            {
                ulong steamID64 = @event.Userid.SteamID;

                if (storage == null)
                {
                    Console.WriteLine("Storage has not been initialized.");
                    Console.WriteLine("Storage has not been initialized.");
                    return HookResult.Continue;
                }
                Server.NextFrame(() =>
                {
                    Task.Run(async () =>
                    {
                        Console.WriteLine("hecho0");
                        await storage.FirstTimeRegister(steamID64);

                    }).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Console.WriteLine($"Error registering player: {task.Exception}");
                            return;
                        }
                        var taskNext = Task.Run(async () => await LoadPlayerSkillsFromDatabase(steamID64));
                        taskNext.ContinueWith(task =>
                        {
                            playerSkillsCache[steamID64] = task.Result;
                        });
                    });
                });
            }
            return HookResult.Continue;
        }
        private async void UpdateSkill(CCSPlayerController player, string skillName, string columnName, CommandInfo commandInfo)
        {
            var steamid = player.SteamID;

            if (!playerSkillsDictionary.TryGetValue(steamid, out PlayerSkills? playerSkills))
            {
                playerSkills = await LoadPlayerSkillsFromDatabase(steamid);
                playerSkillsDictionary[steamid] = playerSkills;
            }

            if (storage != null)
            {
                int currentPoints = await storage.GetPlayerIntAttribute(steamid, columnName);
                int availablePoints = await storage.GetPlayerIntAttribute(steamid, "skillavailable");

                if (availablePoints > 0)
                {
                    if (currentPoints < 10)
                    {
                        //updating values in db
                        await storage.UpdateDb(steamid, columnName, 1);
                        await storage.UpdateDb(steamid, "skillavailable", -1);
                        OnSkillsCommand(player, commandInfo);

                        Server.NextFrame(() =>
                        {
                            player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}You upgraded your {ChatColors.Lime}{skillName} Skill {ChatColors.Gold}to {ChatColors.Lime}{currentPoints + 1}/10.");
                            
                            //updating values in dictionary
                            var task = Task.Run(async () => await LoadPlayerSkillsFromDatabase(steamid));
                            task.Wait();
                            playerSkillsCache[steamid] = task.Result;
                        });
                    }
                    else
                    {
                        Server.NextFrame(() =>
                        {
                            player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}Your skill {ChatColors.Lime}{skillName} {ChatColors.Gold}is already at maximum level.");
                        });
                    }
                }
                if (availablePoints == 0)
                {
                    Server.NextFrame(() =>
                    {
                        player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}You do not have any available skill points to use.");
                    });
                }
            }
        } // updates dictionary too
        public int GetSkillPointsFromDictionary(CCSPlayerController player, string skillName)
        {
            var steamid = player.SteamID;
            if (playerSkillsCache.TryGetValue(steamid, out PlayerSkills? playerSkills))
            {
                switch (skillName.ToLower())
                {
                    case "skill1points":
                        return playerSkills.Skill1Points;
                    case "skill2points":
                        return playerSkills.Skill2Points;
                    case "skill3points":
                        return playerSkills.Skill3Points;
                    case "skill4points":
                        return playerSkills.Skill4Points;
                    case "skill5points":
                        return playerSkills.Skill5Points;
                    default:
                        Console.WriteLine($"Invalid skill name provided: {skillName}");
                        return 0;
                }
            }
            else
            {
                Console.WriteLine($"Skill points for player {steamid} not found in cache.");
                return 0;
            }
        } // gets skill from dictionary to avoid multiple database connection & disconnection on tick
        private HookResult OnPlayerHurtMultiplier(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (@event.Weapon == "hegrenade" && victim != null && victim.Pawn != null && victim.Pawn.Value != null && !attacker.IsBot && IsCT(attacker) && IsT(victim))
            {
                var grenadeDmgPoints = GetSkillPointsFromDictionary(attacker, "skill5points");
                var normaldmg = @event.DmgHealth;
                var newdmg = Math.Pow(normaldmg * grenadeDmgPoints / 15, 2);
                var newdmgfinal = newdmg;
                if (grenadeDmgPoints <= 5 && grenadeDmgPoints >= 1)
                {
                    newdmgfinal = Math.Min(newdmg, 600 - normaldmg + normaldmg / grenadeDmgPoints);
                }
                if (grenadeDmgPoints >= 6 && grenadeDmgPoints <= 8)
                {
                    newdmgfinal = Math.Min(newdmg, 900 - normaldmg + normaldmg / grenadeDmgPoints);
                }
                if (grenadeDmgPoints == 9)
                {
                    newdmgfinal = Math.Min(newdmg, 1050 - normaldmg + normaldmg / grenadeDmgPoints);
                }
                var dmgg = (int)newdmgfinal;
                victim.Pawn.Value.Health -= dmgg;
            }
            if (@event.Weapon == "knife" && victim != null && victim.Pawn != null && victim.Pawn.Value != null && !attacker.IsBot && IsCT(attacker) && IsT(victim))
            {
                var knifeDmgPoints = GetSkillPointsFromDictionary(attacker, "skill4points");
                var normaldmg = @event.DmgHealth;
                var newdmg = normaldmg;
                if (normaldmg <= 50)
                {
                    newdmg += knifeDmgPoints * 10;
                }
                if (normaldmg > 50)
                {
                    newdmg += knifeDmgPoints * 15;
                }
                victim.Pawn.Value.Health -= newdmg - normaldmg;
            }
            return HookResult.Continue;
        } // applies skills points on grenade & knife dmg
        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var jumper = @event.Userid;
            var jumperPawn = jumper.PlayerPawn.Value;
            if (jumper == null || jumper.Pawn == null || jumper.Pawn.Value == null || jumper.IsBot || jumperPawn == null)
            {
                return HookResult.Continue;
            }
            var jumpPoints = GetSkillPointsFromDictionary(jumper, "skill3points");

            if (jumpPoints >= 1)
            {
                var eyeAngle = jumperPawn.EyeAngles;
                var pitch = Math.PI / 180 * eyeAngle.X;
                var yaw = Math.PI / 180 * eyeAngle.Y;
                float jumpFloat = (float)jumpPoints;
                float balancedtimer = (jumpFloat / 250) + 0.16f;
                var timer = new CounterStrikeSharp.API.Modules.Timers.Timer(balancedtimer, () =>
                {
                    jumperPawn.AbsVelocity.Z = 180 + 2 * jumpPoints;
                    var forwardX = Math.Cos(yaw) * Math.Cos(pitch) * (15 + (jumpPoints / 2));
                    var forwardY = Math.Sin(yaw) * Math.Cos(pitch) * (15 + (jumpPoints / 2));
                    jumperPawn.AbsVelocity.X += (float)forwardX;
                    jumperPawn.AbsVelocity.Y += (float)forwardY;
                });
            }
            return HookResult.Continue;
        }

        private HookResult eventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!IsPlayerValid(player))
            {
                return HookResult.Continue;
            }

            Server.NextFrame(() =>
            {
                if (!IsPlayerValid(player))
                {
                    return;
                }
                var playerPawn = player.PlayerPawn.Value;

                if (playerPawn == null) return;

                var hpPoints = GetSkillPointsFromDictionary(player, "skill1points");

                if (hpPoints >= 1)
                {
                    var newHP = playerPawn.Health + (Config.HPIncreasePerLevel * hpPoints);

                    playerPawn.Health = newHP;
                    playerPawn.MaxHealth = newHP;

                    Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                }

                var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                if (speedPoints >= 1)
                {
                    SetPlayerSpeed(playerPawn, speedPoints * Config.SpeedIncreasePerLevel);
                }
            });

            return HookResult.Continue;
        }

        ///// ################### SKILLS END ################### /////


        ///// ################### UTILS ################### /////

        private bool IsCT(CCSPlayerController player)
        {
            return player.TeamNum == 3;
        }
        private bool IsT(CCSPlayerController player)
        {
            return player.TeamNum == 2;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (@event.Userid != null && @event.Userid.IsValid)
            {
                ulong steamid = @event.Userid.SteamID;

                // Remove player data from both dictionaries
                playerSkillsCache.Remove(steamid);
                playerSkillsDictionary.Remove(steamid);
            }
            return HookResult.Continue;
        }

        private bool IsPlayerValid(CCSPlayerController? player)
        {
            return (player != null && player.IsValid && !player.IsBot && !player.IsHLTV && player.PawnIsAlive);
        }

        public void SetPlayerSpeed(CCSPlayerPawn? pawn, float speed)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.VelocityModifier = speed;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flVelocityModifier");
        }
    }
}
