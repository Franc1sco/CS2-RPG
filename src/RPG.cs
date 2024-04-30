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
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands.Targeting;

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
    [JsonPropertyName("JumpIncreasePerLevel")] public float JumpIncreasePerLevel { get; set; } = 0.10f;
    [JsonPropertyName("AdrenalineIncreasePerLevel")] public float AdrenalineIncreasePerLevel { get; set; } = 0.15f;
    [JsonPropertyName("AdrenalineDuration")] public float AdrenalineDuration { get; set; } = 0.5f;
    [JsonPropertyName("AdrenalineOnlyOnHit")] public bool AdrenalineOnlyOnHit { get; set; } = false;
    [JsonPropertyName("NormalKillXP")] public int NormalKillXP { get; set; } = 200;
    [JsonPropertyName("HSKillXP")] public int HSKillXP { get; set; } = 250;
    [JsonPropertyName("KnifeKillXP")] public int KnifeKillXP { get; set; } = 300;
    [JsonPropertyName("GrenadeKillXP")] public int GrenadeKillXP { get; set; } = 750;
    [JsonPropertyName("MaxLevel")] public int MaxLevel { get; set; } = 10;
    [JsonPropertyName("MaxLevelSpeed")] public int MaxLevelSpeed { get; set; } = 40;
    [JsonPropertyName("MaxLevelAdrenaline")] public int MaxLevelAdrenaline { get; set; } = 15;
    [JsonPropertyName("TimeForApplyHP")] public int TimeForApplyHP { get; set; } = 0;
    [JsonPropertyName("ApplyJumpTimer")] public float ApplyJumpTimer { get; set; } = 0.01f;
    [JsonPropertyName("JumpDuration")] public float JumpDuration { get; set; } = 0.5f;
    [JsonPropertyName("CreditsEarnedPerLevel")] public int CreditsEarnedPerLevel { get; set; } = 500;
    [JsonPropertyName("CreditsStart")] public int CreditsStart { get; set; } = 0;
    [JsonPropertyName("UpgradeCostIncreased")] public int UpgradeCostIncreased { get; set; } = 500;
    [JsonPropertyName("DisableHealth")] public bool DisableHealth { get; set; } = false;
    [JsonPropertyName("DisableSpeed")] public bool DisableSpeed { get; set; } = false;
    [JsonPropertyName("DisableJump")] public bool DisableJump { get; set; } = false;
    [JsonPropertyName("DisableKnifeDamage")] public bool DisableKnifeDamage { get; set; } = false;
    [JsonPropertyName("DisableGrenadeDamage")] public bool DisableGrenadeDamage { get; set; } = false;
    [JsonPropertyName("DisableAdrenaline")] public bool DisableAdrenaline { get; set; } = false;
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
        public int Skill6Points { get; set; }
        public int AvailablePoints { get; set; }
        public int Level { get; set; }
        public int PointsCalc { get; set; }



        public int GetSkillPointsByColumnName(string columnName)
        {
            switch (columnName)
            {
                case "skillone": return Skill1Points;
                case "skilltwo": return Skill2Points;
                case "skillthree": return Skill3Points;
                case "skillfour": return Skill4Points;
                case "skillfive": return Skill5Points;
                case "skillsix": return Skill6Points;
                default: throw new ArgumentException("Invalid column name");
            }
        }

        public int GetStatsByColumnName(string columnName)
        {
            switch (columnName)
            {
                case "level": return Level;
                case "pointscalc": return PointsCalc;
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
                case "skillsix": Skill6Points++; break;
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
        public MySQLStorage(string ip, int port, string user, string password, string database, string table, bool isSQLite, string sqliteFilePath, int xPLevel, int startCredits)
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
                skillavailable INTEGER NOT NULL DEFAULT {startCredits},
                skillone INTEGER NOT NULL DEFAULT 0,
                skilltwo INTEGER NOT NULL DEFAULT 0,
                skillthree INTEGER NOT NULL DEFAULT 0,
                skillfour INTEGER NOT NULL DEFAULT 0,
                skillfive INTEGER NOT NULL DEFAULT 0,
                skillsix INTEGER NOT NULL DEFAULT 0,
                skillseven INTEGER NOT NULL DEFAULT 0,
                skilleight INTEGER NOT NULL DEFAULT 0,
                skillnine INTEGER NOT NULL DEFAULT 0,      
                level INTEGER NOT NULL DEFAULT 1
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
                    `skillavailable` bigint NOT NULL DEFAULT {startCredits},
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
                    `rank` bigint NOT NULL DEFAULT 0
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
                if (exists == null)
                {
                    var query = $@"
        INSERT OR IGNORE INTO `{table}` (`steamid`) VALUES (@SteamID);
        ";
                    var command = new SqliteCommand(query, connLocal);
                    command.Parameters.AddWithValue("@SteamID", SteamID);
                    await command.ExecuteNonQueryAsync();
                }
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

        public async Task ResetUser(ulong steamID)
        {
            if (isSQLite)
            {
                try
                {
                    await connLocal.OpenAsync();
                    var sql = $@"DELETE FROM {table} WHERE steamid = @SteamID;";
                    await connLocal.ExecuteAsync(sql, new { SteamID = steamID });
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
                        var sql = $@"DELETE FROM {table} WHERE steamid = @SteamID;";
                        await conn.ExecuteAsync(sql, new { SteamID = steamID });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in UpdateDb: {ex.Message}");
                    }
                }
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
                    var sql = $@"SELECT skillone AS Skill1Points, skilltwo AS Skill2Points, skillthree AS Skill3Points, skillfour AS Skill4Points, skillfive AS Skill5Points, skillsix AS Skill6Points, skillavailable AS AvailablePoints FROM {table} WHERE steamid = @SteamID;";
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
                        var sql = $@"SELECT skillone AS Skill1Points, skilltwo AS Skill2Points, skillthree AS Skill3Points, skillfour AS Skill4Points, skillfive AS Skill5Points, skillsix AS Skill6Points, skillavailable AS AvailablePoints FROM {table} WHERE steamid = @SteamID;";
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
        public override string ModuleVersion => "1.1 - 30/04/2024a";
        public override string ModuleAuthor => "Franc1sco Franug";

        private readonly Dictionary<int?, CounterStrikeSharp.API.Modules.Timers.Timer?> bUsingAdrenaline = new();
        private readonly Dictionary<int?, CounterStrikeSharp.API.Modules.Timers.Timer?> jumping = new();
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
                Config.XPLevel,
                Config.CreditsStart
            );
            base.Load(hotReload);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerConnectDisconnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurtMultiplier, HookMode.Pre);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJump);
            RegisterEventHandler<EventPlayerJump>(OnPlayerJumpPre, HookMode.Pre);
            RegisterEventHandler<EventPlayerSpawn>(eventPlayerSpawn);
            RegisterEventHandler<EventWeaponFire>(eventWeaponFire);

            AddCommand("skills", "Opens menu to upgrade skills", OnSkillsCommand);
            AddCommand("showskills", "show skills in chat", OnShowskillsCommand);
            AddCommand("showstats", "show stats in chat", OnShowstatsCommand);
            AddCommand("sellskills", "sell stats menu", OnSellSkillsCommand);

            AddCommand("rpgmenu", "show rpgmenu", OnRPGCommand);



            if (hotReload)
            {
                AddTimer(2.0f, TimerCheckVelocity, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            RegisterListener<Listeners.OnMapStart>(OnMapStartEvent);

        }

        private HookResult eventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {

            if (Config.AdrenalineOnlyOnHit || Config.DisableAdrenaline) return HookResult.Continue;

            var player = @event.Userid;

            if (!IsPlayerValid(player))
            {
                return HookResult.Continue;
            }

            applyAdrenaline(player);

            return HookResult.Continue;
        }

        private void OnMapStartEvent(string mapName)
        {
            AddTimer(2.0f, TimerCheckVelocity, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void TimerCheckVelocity()
        {
            List<CCSPlayerController> players = Utilities.GetPlayers();

            foreach (CCSPlayerController player in players)
            {
                if (player == null || !player.IsValid || player.IsHLTV || player.SteamID.ToString() == "" || !player.PawnIsAlive
                    || player.IsBot) continue;

                if (storage != null && !playerSkillsCache.ContainsKey(player.SteamID))
                {
                    connectedUser(player);
                    continue;
                }

                if (jumping.ContainsKey(player.UserId) && jumping[player.UserId] == null)
                {
                    var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                    if (speedPoints >= 1 && !Config.DisableSpeed)
                    {
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn == null || !playerPawn.IsValid) return;

                        if (bUsingAdrenaline.ContainsKey(player.UserId) && bUsingAdrenaline[player.UserId] != null)
                        {
                            var adrenalinePoints = GetSkillPointsFromDictionary(player, "skill6points");

                            SetPlayerSpeed(playerPawn, 1.0f + (speedPoints * Config.SpeedIncreasePerLevel + adrenalinePoints * Config.AdrenalineIncreasePerLevel));
                        }
                        else
                        {
                            SetPlayerSpeed(playerPawn, 1.0f + speedPoints * Config.SpeedIncreasePerLevel);
                        }
                    }
                }
            }
        }
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && !@event.Headshot && @event.Weapon != "hegrenade" && @event.Attacker != @event.Userid) // simple kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "kills", 1);
                storage?.UpdateDb(steamid, "points", Config.NormalKillXP);
                storage?.UpdateDb(steamid, "pointscalc", Config.NormalKillXP);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}{Config.NormalKillXP} xp {ChatColors.Gold}for a kill{ChatColors.Gold}.");
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
                storage?.UpdateDb(steamid, "points", Config.HSKillXP);
                storage?.UpdateDb(steamid, "pointscalc", Config.HSKillXP);
                storage?.UpdateDb(steamid, "headshot", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}{Config.HSKillXP} xp {ChatColors.Gold}for a headshot kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }
            /*if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && @event.Weapon == "knife" && IsCT(@event.Attacker) && @event.Attacker != @event.Userid) // knife kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "points", Config.KnifeKillXP);
                storage?.UpdateDb(steamid, "pointscalc", Config.KnifeKillXP);
                storage?.UpdateDb(steamid, "knifekills", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}{Config.KnifeKillXP} xp {ChatColors.Gold}for a knife kill{ChatColors.Gold}.");
                RankUpPlayerOrNo(@event.Attacker);
            }*/
            if (@event.Userid != null && @event.Userid.IsValid && @event.Attacker != null && @event.Attacker.IsValid && !@event.Attacker.IsBot
                && @event.Weapon == "hegrenade" && @event.Attacker != @event.Userid) // grenade kill
            {
                var steamid = @event.Attacker.SteamID;
                storage?.UpdateDb(steamid, "points", Config.GrenadeKillXP);
                storage?.UpdateDb(steamid, "pointscalc", Config.GrenadeKillXP);
                storage?.UpdateDb(steamid, "grenadekills", 1);
                storage?.UpdateDb(steamid, "kills", 1);
                @event.Attacker.PrintToChat($" {ChatColors.Gold}You gained {ChatColors.Lime}{Config.GrenadeKillXP} xp {ChatColors.Gold}for a grenade kill{ChatColors.Gold}.");
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
                    storage?.UpdateDb(steamid, "skillavailable", Config.CreditsEarnedPerLevel);
                    storage?.SetDb(steamid, "pointscalc", 0);
                    if (storage != null)
                    {
                        int level = storage.GetPlayerIntAttribute(steamid, "level").GetAwaiter().GetResult();
                        player.PrintToChat($" {ChatColors.Gold}You have leveled up! You are now {ChatColors.Lime}level {level}{ChatColors.Gold}.");
                        player.PrintToChat($" {ChatColors.Gold}You earned {Config.CreditsEarnedPerLevel} credits for levelling up! Type {ChatColors.Lime}!skills{ChatColors.Gold} to use it.");
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
                    AvailablePoints = 0,
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
                player.PrintToChat($" {ChatColors.Gold}Health: {ChatColors.Lime}{playerSkills.Skill1Points}/{Config.MaxLevel}");
                player.PrintToChat($" {ChatColors.Gold}Speed: {ChatColors.Lime}{playerSkills.Skill2Points}/{Config.MaxLevelSpeed}");
                player.PrintToChat($" {ChatColors.Gold}Jump: {ChatColors.Lime}{playerSkills.Skill3Points}/{Config.MaxLevel}");
                player.PrintToChat($" {ChatColors.Gold}Knife Damage: {ChatColors.Lime}{playerSkills.Skill4Points}/{Config.MaxLevel}");
                player.PrintToChat($" {ChatColors.Gold}Grenade Damage: {ChatColors.Lime}{playerSkills.Skill5Points}/{Config.MaxLevel}");
                player.PrintToChat($" {ChatColors.Gold}Adrenaline: {ChatColors.Lime}{playerSkills.Skill6Points}/{Config.MaxLevelAdrenaline}");
                player.PrintToChat($" {ChatColors.Green}Available Skill Points: {ChatColors.Lime}{playerSkills.AvailablePoints}");
            }
            else
            {
                // If not found in cache, you might want to load from database or display an error
                // This is just an error message, implement loading from DB as needed
                player.PrintToChat($"{ChatColors.Red}Error: Could not retrieve your skills. Please try again.");
            }
        } // for debugging, to see if dictionary contains the right values

        private void OnShowstatsCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot)
            {
                return; // Check if the player object is valid and not a bot
            }

            var steamid = player.SteamID;
            if (storage != null)
            {
                int level = storage.GetPlayerIntAttribute(steamid, "level").GetAwaiter().GetResult();
                int pointscalc = storage.GetPlayerIntAttribute(steamid, "pointscalc").GetAwaiter().GetResult();
                // Attempt to retrieve the player's skills from the cache
                if (playerSkillsCache.TryGetValue(steamid, out PlayerSkills? playerSkills))
                {
                    // If found, print each stat and its xp
                    player.PrintToChat($"{ChatColors.Green}[Skills]{ChatColors.Gold} Your current stats:");
                    player.PrintToChat($" {ChatColors.Gold} Your current lvl is: {level}");
                    player.PrintToChat($" {ChatColors.Gold} Your current xp is: {pointscalc} / {Config.XPLevel}");
                }
                else
                {
                    // If not found in cache, you might want to load from database or display an error
                    // This is just an error message, implement loading from DB as needed
                    player.PrintToChat($"{ChatColors.Red}Error: Could not retrieve your skills. Please try again.");
                }
            }
        }
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
                int adrenalinePoints = await storage.GetPlayerIntAttribute(steamid, "skillsix");


                Server.NextFrame(() =>
                {
                    CenterHtmlMenu menu = new CenterHtmlMenu($"Skills Menu", this);
                    menu.Title = $"<font color='lightblue'>Available Credits : <font color='pink'>{availablePoints}<br><font color='yellow'>Upgrade your Skills :<font color='white'><font color='white'>";
                    if (!Config.DisableHealth) menu.AddMenuOption($"Health [{healthPoints}/{Config.MaxLevel}] {healthPoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Health", "skillone", commandInfo), availablePoints < (healthPoints * Config.UpgradeCostIncreased));
                    if (!Config.DisableSpeed) menu.AddMenuOption($"Speed [{speedPoints}/{Config.MaxLevelSpeed}] {speedPoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Speed", "skilltwo", commandInfo), availablePoints < (speedPoints * Config.UpgradeCostIncreased));
                    if (!Config.DisableJump) menu.AddMenuOption($"Jump [{jumpPoints}/{Config.MaxLevel}] {jumpPoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Jump", "skillthree", commandInfo), availablePoints < (jumpPoints * Config.UpgradeCostIncreased));
                    if (!Config.DisableKnifeDamage) menu.AddMenuOption($"Knife Damage [{knifeDmgPoints}/{Config.MaxLevel}] {knifeDmgPoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Knife Damage", "skillfour", commandInfo), availablePoints < (knifeDmgPoints * Config.UpgradeCostIncreased));
                    if (!Config.DisableGrenadeDamage) menu.AddMenuOption($"Grenade Damage [{GrenaeDmgPoints}/{Config.MaxLevel}] {GrenaeDmgPoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Grenade Damage", "skillfive", commandInfo), availablePoints < (GrenaeDmgPoints * Config.UpgradeCostIncreased));
                    if (!Config.DisableAdrenaline) menu.AddMenuOption($"Adrenaline [{adrenalinePoints}/{Config.MaxLevelAdrenaline}] {adrenalinePoints * Config.UpgradeCostIncreased} credits", (p, option) => UpdateSkill(p, "Adrenaline", "skillsix", commandInfo), availablePoints < (adrenalinePoints * Config.UpgradeCostIncreased));
                    MenuManager.OpenCenterHtmlMenu(this, player, menu);
                });
            }
        }
        private async void OnSellSkillsCommand(CCSPlayerController? player, CommandInfo commandInfo)
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
                int adrenalinePoints = await storage.GetPlayerIntAttribute(steamid, "skillsix");

                Server.NextFrame(() =>
                {
                    CenterHtmlMenu menu = new CenterHtmlMenu($"Sell Skills Menu", this);
                    menu.Title = $"<font color='lightblue'>Available Credits : <font color='pink'>{availablePoints}<br><font color='yellow'>Sell your Skills :<font color='white'><font color='white'>";
                    if (!Config.DisableHealth) menu.AddMenuOption($"Health [{healthPoints}/{Config.MaxLevel}]", (p, option) => SellSkill(p, "Health", "skillone", commandInfo));
                    if (!Config.DisableSpeed) menu.AddMenuOption($"Speed [{speedPoints}/{Config.MaxLevelSpeed}]", (p, option) => SellSkill(p, "Speed", "skilltwo", commandInfo));
                    if (!Config.DisableJump) menu.AddMenuOption($"Jump [{jumpPoints}/{Config.MaxLevel}]", (p, option) => SellSkill(p, "Jump", "skillthree", commandInfo));
                    if (!Config.DisableKnifeDamage) menu.AddMenuOption($"Knife Damage [{knifeDmgPoints}/{Config.MaxLevel}]", (p, option) => SellSkill(p, "Knife Damage", "skillfour", commandInfo));
                    if (!Config.DisableGrenadeDamage) menu.AddMenuOption($"Grenade Damage [{GrenaeDmgPoints}/{Config.MaxLevel}]", (p, option) => SellSkill(p, "Grenade Damage", "skillfive", commandInfo));
                    if (!Config.DisableAdrenaline) menu.AddMenuOption($"Adrenaline [{adrenalinePoints}/{Config.MaxLevelAdrenaline}]", (p, option) => SellSkill(p, "Adrenaline", "skillsix", commandInfo));
                    MenuManager.OpenCenterHtmlMenu(this, player, menu);
                });
            }
        }
        private void OnRPGCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;

            CenterHtmlMenu menu = new CenterHtmlMenu($"RPG Menu", this);
            menu.AddMenuOption("Open Skills menu", (p, option) => {
                OnSkillsCommand(player, null);
            });
            menu.AddMenuOption("Show Skills", (p, option) => {
                OnShowskillsCommand(player, null);
            });
            menu.AddMenuOption("Sell Skills", (p, option) => {
                OnSellSkillsCommand(player, null);
            });

            menu.AddMenuOption("Show Stats", (p, option) => {
                OnShowstatsCommand(player, null);
            });
            menu.AddMenuOption("Reset RPG", (p, option) => {
                var confirmationMenu = new CenterHtmlMenu("Confirmation", this);
                confirmationMenu.AddMenuOption("Confirm", (confirmationPlayer, _) => {
                    Server.NextFrame(() =>
                    {
                        Task.Run(async () =>
                        {
                            await storage.ResetUser(confirmationPlayer.SteamID);

                        }).ContinueWith(task =>
                        {
                            Task.Run(async () =>
                            {
                                await storage.FirstTimeRegister(confirmationPlayer.SteamID);

                            }).ContinueWith(task =>
                            {
                                if (task.Exception != null)
                                {
                                    Console.WriteLine($"Error registering player: {task.Exception}");
                                    return;
                                }
                                var taskNext = Task.Run(async () => await LoadPlayerSkillsFromDatabase(confirmationPlayer.SteamID));
                                taskNext.ContinueWith(task =>
                                {
                                    playerSkillsCache[confirmationPlayer.SteamID] = task.Result;
                                });
                            });
                        });
                        confirmationPlayer.PrintToChat($" {ChatColors.Green}[Skills] you reset your level.");
                        MenuManager.CloseActiveMenu(confirmationPlayer);
                    });
                });

                MenuManager.OpenCenterHtmlMenu(this, player, confirmationMenu);
            });

            MenuManager.OpenCenterHtmlMenu(this, player, menu);
        }


        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot)
            {
                connectedUser(player);
            }
            return HookResult.Continue;
        }

        private void connectedUser(CCSPlayerController player)
        {
            bUsingAdrenaline.Add(player.UserId, null);
            jumping.Add(player.UserId, null);
            ulong steamID64 = player.SteamID;

            if (storage == null)
            {

                Console.WriteLine("Storage has not been initialized.");
                Console.WriteLine("Storage has not been initialized.");
                return;
            }

            Server.NextFrame(() =>
            {
                Task.Run(async () =>
                {
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
        private HookResult OnPlayerConnectDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot)
            {
                if (bUsingAdrenaline.ContainsKey(player.UserId))
                {
                    bUsingAdrenaline.Remove(player.UserId);
                }

                if (jumping.ContainsKey(player.UserId))
                {
                    jumping.Remove(player.UserId);
                }
            }
            return HookResult.Continue;
        }

        private async void SellSkill(CCSPlayerController player, string skillName, string columnName, CommandInfo commandInfo)
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

                if (currentPoints > 0)
                {
                    // Refund points
                    var price = currentPoints * Config.UpgradeCostIncreased;
                    await storage.UpdateDb(steamid, columnName, -1);
                    await storage.UpdateDb(steamid, "skillavailable", price);
                    OnSellSkillsCommand(player, commandInfo);

                    Server.NextFrame(() =>
                    {
                        player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}You sold your {ChatColors.Lime}{skillName} Skill {ChatColors.Gold}and got back {price} credits.");

                        // Update dictionary
                        var task = Task.Run(async () => await LoadPlayerSkillsFromDatabase(steamid));
                        task.Wait();
                        playerSkillsCache[steamid] = task.Result;
                    });
                }
                else
                {
                    Server.NextFrame(() =>
                    {
                        player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}Your skill {ChatColors.Lime}{skillName} {ChatColors.Gold}is already at minimum level.");
                    });
                }
            }
        }

        private int GetMaxLevel(string columnName)
        {
            switch (columnName)
            {
                case "skilltwo":
                    return Config.MaxLevelSpeed;
                case "skillsix":
                    return Config.MaxLevelAdrenaline;
                default:
                    return Config.MaxLevel;
            }
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

                if (availablePoints >= currentPoints * Config.UpgradeCostIncreased)
                {
                    if (currentPoints < GetMaxLevel(columnName))
                    {
                        //updating values in db
                        await storage.UpdateDb(steamid, columnName, 1);
                        await storage.UpdateDb(steamid, "skillavailable", -(currentPoints * Config.UpgradeCostIncreased));
                        OnSkillsCommand(player, commandInfo);

                        Server.NextFrame(() =>
                        {
                            player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}You upgraded your {ChatColors.Lime}{skillName} Skill {ChatColors.Gold}to {ChatColors.Lime}{currentPoints + 1}/{GetMaxLevel(columnName)}.");

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
                else
                {
                    Server.NextFrame(() =>
                    {
                        player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}You do not have enought credits to use.");
                    });
                }
            }
        } // updates dictionary too
        public int GetSkillPointsFromDictionary(CCSPlayerController player, string skillName)
        {
            var steamid = player.SteamID;
            if (playerSkillsCache.ContainsKey(steamid) && playerSkillsCache.TryGetValue(steamid, out PlayerSkills? playerSkills))
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
                    case "skill6points":
                        return playerSkills.Skill6Points;
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

            if (!IsPlayerValid(attacker) || !IsPlayerValid(victim) || victim.UserId == attacker.UserId)
                return HookResult.Continue;

            if (Config.AdrenalineOnlyOnHit)
                applyAdrenaline(attacker);

            if (!Config.DisableGrenadeDamage && @event.Weapon == "hegrenade")
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
            else if (!Config.DisableKnifeDamage && @event.Weapon == "knife")
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

        private HookResult OnPlayerJumpPre(EventPlayerJump @event, GameEventInfo info)
        {
            var jumper = @event.Userid;
            var jumperPawn = jumper.PlayerPawn.Value;
            if (jumper == null || jumper.Pawn == null || jumper.Pawn.Value == null || jumper.IsBot || jumperPawn == null)
            {
                return HookResult.Continue;
            }
            var jumpPoints = GetSkillPointsFromDictionary(jumper, "skill3points");

            if (jumpPoints >= 1 && !Config.DisableJump)
            {
                SetPlayerSpeed(jumperPawn, 1.0f);

                if (jumping[jumper.UserId] != null) jumping[jumper.UserId]?.Kill();

                jumping[jumper.UserId] = AddTimer(Config.JumpDuration, () =>
                {
                    jumping[jumper.UserId] = null;

                });
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            var jumper = @event.Userid;
            var jumperPawn = jumper.PlayerPawn.Value;
            if (jumper == null || jumper.Pawn == null || jumper.Pawn.Value == null || jumper.IsBot || jumperPawn == null)
            {
                return HookResult.Continue;
            }
            var jumpPoints = GetSkillPointsFromDictionary(jumper, "skill3points");

            if (jumpPoints >= 1 && !Config.DisableJump)
            {
                SetPlayerSpeed(jumperPawn, 1.0f);
                AddTimer(Config.ApplyJumpTimer, () =>
                {
                    SetPlayerSpeed(jumperPawn, 1.0f);

                    var increase = Config.JumpIncreasePerLevel * jumpPoints + 1.0;

                    jumperPawn.AbsVelocity.X *= (float)increase;
                    jumperPawn.AbsVelocity.Y *= (float)increase;
                    //jumperPawn.AbsVelocity.Z *= (float)increase;
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

            if (Config.TimeForApplyHP == 0)
            {
                Server.NextFrame(() =>
                {
                    if (!IsPlayerValid(player))
                    {
                        return;
                    }
                    var playerPawn = player.PlayerPawn.Value;

                    if (playerPawn == null) return;

                    var hpPoints = GetSkillPointsFromDictionary(player, "skill1points");

                    if (hpPoints >= 1 && !Config.DisableHealth)
                    {
                        var newHP = playerPawn.Health + (Config.HPIncreasePerLevel * hpPoints);

                        playerPawn.Health = newHP;
                        playerPawn.MaxHealth = newHP;

                        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                    }

                    var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                    if (speedPoints >= 1 && !Config.DisableSpeed)
                    {
                        SetPlayerSpeed(playerPawn, 1.0f + speedPoints * Config.SpeedIncreasePerLevel);
                    }
                });
            } 
            else
            {
                AddTimer(Config.TimeForApplyHP * 1.0f, () =>
                {
                    if (!IsPlayerValid(player))
                    {
                        return;
                    }
                    var playerPawn = player.PlayerPawn.Value;

                    if (playerPawn == null) return;

                    var hpPoints = GetSkillPointsFromDictionary(player, "skill1points");

                    if (hpPoints >= 1 && !Config.DisableHealth)
                    {
                        var newHP = playerPawn.Health + (Config.HPIncreasePerLevel * hpPoints);

                        playerPawn.Health = newHP;
                        playerPawn.MaxHealth = newHP;

                        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
                    }

                    var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                    if (speedPoints >= 1 && !Config.DisableSpeed)
                    {
                        SetPlayerSpeed(playerPawn, 1.0f + speedPoints * Config.SpeedIncreasePerLevel);
                    }
                });
            }

            return HookResult.Continue;
        }

        private void applyAdrenaline(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null) return;

            if (jumping.ContainsKey(player.UserId) && jumping[player.UserId] != null) return;

            var adrenalinePoints = GetSkillPointsFromDictionary(player, "skill6points");

            if (adrenalinePoints >= 1 && !Config.DisableAdrenaline)
            {
                var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");
                if (speedPoints >= 1)
                {
                    SetPlayerSpeed(playerPawn, 1.0f + (speedPoints * Config.SpeedIncreasePerLevel + adrenalinePoints * Config.AdrenalineIncreasePerLevel));
                }
                else
                {
                    SetPlayerSpeed(playerPawn, 1.0f + adrenalinePoints * Config.AdrenalineIncreasePerLevel);
                }

                if (bUsingAdrenaline[player.UserId] != null) bUsingAdrenaline[player.UserId]?.Kill();

                bUsingAdrenaline[player.UserId] = AddTimer(Config.AdrenalineDuration, () =>
                {
                    bUsingAdrenaline[player.UserId] = null;

                    if (!IsPlayerValid(player))
                    {
                        return;
                    }
                    if (playerPawn == null || !playerPawn.IsValid) return;

                    var speedPoints = GetSkillPointsFromDictionary(player, "skill2points");

                    if (speedPoints >= 1)
                    {
                        SetPlayerSpeed(playerPawn, 1.0f + speedPoints * Config.SpeedIncreasePerLevel);
                    }
                    else
                    {
                        SetPlayerSpeed(playerPawn, 1.0f);
                    }
                });
            }
        }
        ///// ################### SKILLS END ################### /////
        ///
        ///// ################### COMMANDS ################### /////
        [ConsoleCommand("css_givexp")]
        [RequiresPermissions("@css/slay")]
        [CommandHelper(minArgs: 1, usage: "<#userid or name> <xp>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnGiveXpCommand(CCSPlayerController? caller, CommandInfo command)
        {
            string callerName = caller == null ? "Console" : caller.PlayerName;
            int value = 0;
            int.TryParse(command.GetArg(2), out value);

            TargetResult? targets = GetTarget(command);
            if (targets == null) return;

            List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

            playersToTarget.ForEach(player =>
            {
                if (!player.IsBot && player.SteamID.ToString().Length != 17)
                    return;

                var steamid = player.SteamID;
                storage?.UpdateDb(steamid, "points", value);
                storage?.UpdateDb(steamid, "pointscalc", value);
                player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}you received {value} exp by {callerName}");
                RankUpPlayerOrNo(player);
            });
        }

        [ConsoleCommand("css_givelevel")]
        [RequiresPermissions("@css/slay")]
        [CommandHelper(minArgs: 1, usage: "<#userid or name> <level>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnGiveLevelCommand(CCSPlayerController? caller, CommandInfo command)
        {
            string callerName = caller == null ? "Console" : caller.PlayerName;
            int value = 0;
            int.TryParse(command.GetArg(2), out value);

            TargetResult? targets = GetTarget(command);
            if (targets == null) return;

            List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

            playersToTarget.ForEach(player =>
            {
                if (!player.IsBot && player.SteamID.ToString().Length != 17)
                    return;

                var steamid = player.SteamID;

                storage?.UpdateDb(steamid, "level", value);
                storage?.UpdateDb(steamid, "skillavailable", value);
                storage?.SetDb(steamid, "pointscalc", 0);

                player.PrintToChat($" {ChatColors.Green}[Skills] {ChatColors.Gold}you received {value} levels by {callerName}");
            });
        }


        ///// ################### COMMANDS END ################### /////
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
            //pawn.VelocityModifier = speed;
            //Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flVelocityModifier");

            //Console.WriteLine("Valor es " + speed);

            pawn.VelocityModifier = speed;
            pawn.GravityScale = speed;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flVelocityModifier");
            //Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flGravityScale");
        }

        private static TargetResult? GetTarget(CommandInfo command)
        {
            TargetResult matches = command.GetArgTargetResult(1);

            if (!matches.Any())
            {
                command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
                return null;
            }

            if (command.GetArg(1).StartsWith('@'))
                return matches;

            if (matches.Count() == 1)
                return matches;

            command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
            return null;
        }
    }
}

public struct WeaponSpeedStats
{
    public double Running { get; }
    public double Walking { get; }

    public WeaponSpeedStats(double running, double walking)
    {
        Running = running;
        Walking = walking;
    }

    public double GetSpeed(bool isWalking)
    {
        return isWalking ? Walking : Running;
    }
}
