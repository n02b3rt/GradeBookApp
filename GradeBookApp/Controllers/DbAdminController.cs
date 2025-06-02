using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GradeBookApp.Api.Controllers
{
    // DTO do odczytu JSON-a z frontu
    public class SwitchRequest
    {
        public bool UseBackup { get; set; }
    }

    [ApiController]
    [Route("api/db")]
    public class DbAdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<DatabaseSettings> _dbSettingsMonitor;
        private readonly string _primaryConn;
        private readonly string _backupConn;
        private readonly string _appSettingsPath;

        public DbAdminController(
            IConfiguration configuration,
            IOptionsMonitor<DatabaseSettings> dbSettingsMonitor,
            IWebHostEnvironment env)
        {
            _configuration = configuration;
            _dbSettingsMonitor = dbSettingsMonitor;

            // Pobieramy connection stringi
            _primaryConn = _configuration.GetConnectionString("Primary")
                           ?? throw new InvalidOperationException("Brak connection string 'Primary'");
            _backupConn = _configuration.GetConnectionString("Backup")
                          ?? throw new InvalidOperationException("Brak connection string 'Backup'");

            Console.WriteLine($"[DbAdminController] Konstruktor: PrimaryConn = {_primaryConn}");
            Console.WriteLine($"[DbAdminController] Konstruktor: BackupConn  = {_backupConn}");

            // Ścieżka do pliku appsettings.json (ContentRoot)
            _appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
            Console.WriteLine($"[DbAdminController] Konstruktor: appSettingsPath = {_appSettingsPath}");
        }

        /// <summary>
        /// GET api/db/status
        /// Zwraca aktualną wartość UseBackup (poprzez IOptionsMonitor)
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            bool useBackup = _dbSettingsMonitor.CurrentValue.UseBackup;
            Console.WriteLine($"[DbAdminController] GetStatus: UseBackup = {useBackup}");
            return Ok(new { UseBackup = useBackup });
        }

        /// <summary>
        /// POST api/db/sync
        /// 1. Rozłącza istniejące sesje do bazy „backup" (jeśli są),
        /// 2. Dropuje bazę „backup",
        /// 3. Tworzy nową bazę „backup" jako TEMPLATE „primary",
        /// 4. Czyści wszystkie pule połączeń Npgsql.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncBackup()
        {
            Console.WriteLine("[DbAdminController] SyncBackup: wywołane");
            try
            {
                var primaryBuilder = new NpgsqlConnectionStringBuilder(_primaryConn);
                var backupBuilder = new NpgsqlConnectionStringBuilder(_backupConn);

                string primaryDbName = primaryBuilder.Database;
                string backupDbName = backupBuilder.Database;

                Console.WriteLine($"[DbAdminController] SyncBackup: primaryDbName = {primaryDbName}");
                Console.WriteLine($"[DbAdminController] SyncBackup: backupDbName  = {backupDbName}");

                // Połącz się do bazy „postgres" (aby móc DROP/CREATE)
                var adminBuilder = new NpgsqlConnectionStringBuilder(_primaryConn)
                {
                    Database = "postgres"
                };
                Console.WriteLine($"[DbAdminController] SyncBackup: adminConn = {adminBuilder.ConnectionString}");

                await using (var conn = new NpgsqlConnection(adminBuilder.ConnectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("[DbAdminController] SyncBackup: Połączono do postgres");

                    // 0) Rozłącz wszystkie sesje do Primary (żeby utworzyć TEMPLATE)
                    string disconnectPrimarySql = $@"
                        SELECT pg_terminate_backend(pid)
                        FROM pg_stat_activity
                        WHERE datname = '{primaryDbName}'
                          AND pid <> pg_backend_pid();";
                    Console.WriteLine($"[DbAdminController] SyncBackup: disconnectPrimarySql = {disconnectPrimarySql.Trim()}");
                    await using (var cmdDiscP = new NpgsqlCommand(disconnectPrimarySql, conn))
                    {
                        int terminatedP = await cmdDiscP.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DbAdminController] SyncBackup: Rozłączono sesji w Primary: {terminatedP}");
                    }

                    // 1) Rozłącz wszystkie połączenia do backupDbName
                    string disconnectBackupSql = $@"
                        SELECT pg_terminate_backend(pid)
                        FROM pg_stat_activity
                        WHERE datname = '{backupDbName}'
                          AND pid <> pg_backend_pid();";
                    Console.WriteLine($"[DbAdminController] SyncBackup: disconnectBackupSql = {disconnectBackupSql.Trim()}");
                    await using (var cmdDiscB = new NpgsqlCommand(disconnectBackupSql, conn))
                    {
                        int terminatedB = await cmdDiscB.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DbAdminController] SyncBackup: Rozłączono sesji w Backup: {terminatedB}");
                    }

                    // 2) DROP DATABASE IF EXISTS backupDbName
                    string dropSql = $"DROP DATABASE IF EXISTS \"{backupDbName}\";";
                    Console.WriteLine($"[DbAdminController] SyncBackup: dropSql = {dropSql}");
                    await using (var cmdDrop = new NpgsqlCommand(dropSql, conn))
                    {
                        await cmdDrop.ExecuteNonQueryAsync();
                        Console.WriteLine("[DbAdminController] SyncBackup: DROP wykonane");
                    }

                    // 3) CREATE DATABASE backupDbName TEMPLATE primaryDbName
                    string createSql = $"CREATE DATABASE \"{backupDbName}\" TEMPLATE \"{primaryDbName}\";";
                    Console.WriteLine($"[DbAdminController] SyncBackup: createSql = {createSql}");
                    await using (var cmdCreate = new NpgsqlCommand(createSql, conn))
                    {
                        await cmdCreate.ExecuteNonQueryAsync();
                        Console.WriteLine("[DbAdminController] SyncBackup: CREATE wykonane");
                    }
                }

                // 4) Po DROP/CREATE czyścimy pule połączeń Npgsql
                NpgsqlConnection.ClearAllPools();
                Console.WriteLine("[DbAdminController] SyncBackup: Wyczyść puli połączeń Npgsql (ClearAllPools)");

                Console.WriteLine("[DbAdminController] SyncBackup: zakończono pomyślnie");
                return Ok(new { message = $"Backup '{backupDbName}' zsynchronizowany z '{primaryDbName}'." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbAdminController] SyncBackup: wyjątek = {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST api/db/switch
        /// Ustawia wartość UseBackup = true/false w appsettings.json,
        /// następnie przeładowuje konfigurację, czyści pule połączeń i KOŃCZY PROCES.
        /// </summary>
        [HttpPost("switch")]
        public IActionResult SwitchDatabase([FromBody] SwitchRequest request)
        {
            try
            {
                bool useBackup = request.UseBackup;
                Console.WriteLine($"[DbAdminController] SwitchDatabase: wywołane; UseBackup = {useBackup}");
                Console.WriteLine($"[DbAdminController] SwitchDatabase: appSettingsPath = {_appSettingsPath}");

                // 1. Odczyt pliku appsettings.json
                string json = System.IO.File.ReadAllText(_appSettingsPath);
                var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("Nie udało się sparsować appsettings.json");

                // 2. Zaktualizuj lub utwórz sekcję DatabaseSettings
                if (jsonObj.TryGetValue("DatabaseSettings", out var dbSectionObj)
                    && dbSectionObj is JsonElement elem
                    && elem.ValueKind == JsonValueKind.Object)
                {
                    var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(elem.GetRawText())
                                   ?? new Dictionary<string, object>();
                    existing["UseBackup"] = useBackup;
                    jsonObj["DatabaseSettings"] = existing;
                    Console.WriteLine($"[DbAdminController] SwitchDatabase: zaktualizowano UseBackup = {useBackup}");
                }
                else
                {
                    jsonObj["DatabaseSettings"] = new Dictionary<string, object>
                    {
                        ["UseBackup"] = useBackup
                    };
                    Console.WriteLine($"[DbAdminController] SwitchDatabase: utworzono sekcję DatabaseSettings = {{\"UseBackup\":{useBackup}}}");
                }

                // 3. Serializacja i zapis pliku (appsettings.json)
                var newJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_appSettingsPath, newJson);

                // ** Dodatkowy log – zawartość pliku po zapisie **
                Console.WriteLine($"[DbAdminController] SwitchDatabase: (po zapisie) zawartość pliku:\n{System.IO.File.ReadAllText(_appSettingsPath)}");

                // 4. TUTAJ WYMUSZAMY RELOAD KONFIGURACJI
                if (_configuration is IConfigurationRoot configRoot)
                {
                    configRoot.Reload();
                    Console.WriteLine("[DbAdminController] SwitchDatabase: config.Reload() wykonane");
                }

                // 5. TUTAJ WYCZYŚCIĆ PULE POŁĄCZEŃ
                NpgsqlConnection.ClearAllPools();
                Console.WriteLine("[DbAdminController] SwitchDatabase: Npgsql.ClearAllPools() wykonane");

                // 6. Zakończ proces, aby przy ponownym uruchomieniu wykonać migracje + seedy
                Console.WriteLine("[DbAdminController] SwitchDatabase: zakończenie procesu (Environment.Exit)");
                Environment.Exit(0);

                // → nigdy tu nie dotrzemy, ale kompilator wymaga return
                return Ok(new { UseBackup = useBackup });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbAdminController] SwitchDatabase: wyjątek = {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
