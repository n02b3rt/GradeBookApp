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

            // Pobierz connection stringi
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
        /// POST api/db/switch
        /// Ustawia wartość UseBackup = true/false w appsettings.json,
        /// następnie przeładowuje konfigurację, czyści pule połączeń i kończy proces.
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

                // 6. Zakończ proces, aby przy kolejnym uruchomieniu wykonać migracje + seedy
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

        /// <summary>
        /// POST api/db/sync
        /// Synchronizuje bazę „aktywna” → „druga”:
        /// - jeśli UseBackup = false, to Primary → Backup,
        /// - jeśli UseBackup = true, to Backup → Primary.
        ///
        /// Kroki:
        /// 1. Rozłącz wszystkie sesje do obu baz (żeby można było DROP/CREATE),
        /// 2. DROP docelowej bazy,
        /// 3. CREATE docelowej bazy jako TEMPLATE źródłowej,
        /// 4. ClearAllPools().
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncBasedOnSetting()
        {
            Console.WriteLine("[DbAdminController] SyncBasedOnSetting: wywołane");
            bool useBackup = _dbSettingsMonitor.CurrentValue.UseBackup;
            Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: UseBackup = {useBackup}");

            // Która baza jest źródłowa, a która docelowa?
            string sourceConn = useBackup ? _backupConn : _primaryConn;
            string targetConn = useBackup ? _primaryConn : _backupConn;

            var sourceBuilder = new NpgsqlConnectionStringBuilder(sourceConn);
            var targetBuilder = new NpgsqlConnectionStringBuilder(targetConn);

            string sourceDbName = sourceBuilder.Database;
            string targetDbName = targetBuilder.Database;

            Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: sourceDbName = {sourceDbName}");
            Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: targetDbName = {targetDbName}");

            try
            {
                // Połącz z „postgres” na dowolnej bazie (wykorzystamy sourceConn, ale ustawiamy Database = "postgres")
                var adminBuilder = new NpgsqlConnectionStringBuilder(sourceConn)
                {
                    Database = "postgres"
                };
                Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: adminConn = {adminBuilder.ConnectionString}");

                await using (var conn = new NpgsqlConnection(adminBuilder.ConnectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("[DbAdminController] SyncBasedOnSetting: Połączono do postgres");

                    // 1) Rozłącz sesje (!) do obu baz (source i target)
                    string disconnectSourceSql = $@"
                        SELECT pg_terminate_backend(pid)
                        FROM pg_stat_activity
                        WHERE datname = '{sourceDbName}'
                          AND pid <> pg_backend_pid();";
                    Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: disconnectSourceSql = {disconnectSourceSql.Trim()}");
                    await using (var cmdDiscSrc = new NpgsqlCommand(disconnectSourceSql, conn))
                    {
                        int terminatedSrc = await cmdDiscSrc.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: Rozłączono sesji w Source: {terminatedSrc}");
                    }

                    string disconnectTargetSql = $@"
                        SELECT pg_terminate_backend(pid)
                        FROM pg_stat_activity
                        WHERE datname = '{targetDbName}'
                          AND pid <> pg_backend_pid();";
                    Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: disconnectTargetSql = {disconnectTargetSql.Trim()}");
                    await using (var cmdDiscTgt = new NpgsqlCommand(disconnectTargetSql, conn))
                    {
                        int terminatedTgt = await cmdDiscTgt.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: Rozłączono sesji w Target: {terminatedTgt}");
                    }

                    // 2) DROP DATABASE IF EXISTS targetDbName
                    string dropSql = $"DROP DATABASE IF EXISTS \"{targetDbName}\";";
                    Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: dropSql = {dropSql}");
                    await using (var cmdDrop = new NpgsqlCommand(dropSql, conn))
                    {
                        await cmdDrop.ExecuteNonQueryAsync();
                        Console.WriteLine("[DbAdminController] SyncBasedOnSetting: DROP wykonane");
                    }

                    // 3) CREATE DATABASE targetDbName TEMPLATE sourceDbName
                    string createSql = $"CREATE DATABASE \"{targetDbName}\" TEMPLATE \"{sourceDbName}\";";
                    Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: createSql = {createSql}");
                    await using (var cmdCreate = new NpgsqlCommand(createSql, conn))
                    {
                        await cmdCreate.ExecuteNonQueryAsync();
                        Console.WriteLine("[DbAdminController] SyncBasedOnSetting: CREATE wykonane");
                    }
                }

                // 4) Wyczyść pule połączeń Npgsql
                NpgsqlConnection.ClearAllPools();
                Console.WriteLine("[DbAdminController] SyncBasedOnSetting: ClearAllPools wykonane");

                return Ok(new
                {
                    message = useBackup
                        ? $"Zsynchronizowano backup → primary: '{sourceDbName}' → '{targetDbName}'."
                        : $"Zsynchronizowano primary → backup: '{sourceDbName}' → '{targetDbName}'."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbAdminController] SyncBasedOnSetting: wyjątek = {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
