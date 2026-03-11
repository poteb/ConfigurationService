using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pote.Config.DbModel;

const string connectionString = "Server=localhost;Database=ConfigurationService;User Id=sa;Password=WaggaMama!1;TrustServerCertificate=True;Encrypt=False";
const string fileDbPath = @"D:\ConfigurationDatabase";
const string scriptsPath = @"D:\git\ConfigurationService\src\Config.DataProvider.SqlServer\CreateScripts";

// Step 1: Run CREATE scripts
Console.WriteLine("=== Running CREATE scripts ===");
await RunCreateScripts();

// Step 2: Migrate data
Console.WriteLine("\n=== Migrating data ===");
await MigrateData();

Console.WriteLine("\n=== Migration complete! ===");

async Task RunCreateScripts()
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var scripts = Directory.GetFiles(scriptsPath, "*.sql").OrderBy(f => f).ToList();
    foreach (var script in scripts)
    {
        var fileName = Path.GetFileName(script);
        var sql = await File.ReadAllTextAsync(script);
        try
        {
            // Split on GO if present, otherwise execute as-is
            var batches = sql.Split("\nGO\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                    await conn.ExecuteAsync(batch);
            }
            Console.WriteLine($"  OK: {fileName}");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"  FAIL: {fileName} - {ex.Message}");
        }
    }
}

async Task MigrateData()
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    // Migrate Applications
    var appDir = Path.Combine(fileDbPath, "applications");
    var appFiles = Directory.GetFiles(appDir, "*.txt").Where(f => !Path.GetFileName(f).StartsWith("AuditLog")).ToList();
    Console.WriteLine($"\nMigrating {appFiles.Count} applications...");
    foreach (var file in appFiles)
    {
        var app = JsonConvert.DeserializeObject<Application>(await File.ReadAllTextAsync(file));
        if (app == null) continue;
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES (@Id, @Name)", new { app.Id, app.Name });
        Console.WriteLine($"  App: {app.Name} ({app.Id})");
    }

    // Migrate Environments
    var envDir = Path.Combine(fileDbPath, "environments");
    var envFiles = Directory.GetFiles(envDir, "*.txt").Where(f => !Path.GetFileName(f).StartsWith("AuditLog")).ToList();
    Console.WriteLine($"\nMigrating {envFiles.Count} environments...");
    foreach (var file in envFiles)
    {
        var env = JsonConvert.DeserializeObject<pote.Config.DbModel.Environment>(await File.ReadAllTextAsync(file));
        if (env == null) continue;
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES (@Id, @Name)", new { env.Id, env.Name });
        Console.WriteLine($"  Env: {env.Name} ({env.Id})");
    }

    // Migrate Configurations
    var configDir = Path.Combine(fileDbPath, "configurations");
    var configFiles = Directory.GetFiles(configDir, "*.txt").ToList();
    Console.WriteLine($"\nMigrating {configFiles.Count} configuration headers...");
    foreach (var file in configFiles)
    {
        var json = await File.ReadAllTextAsync(file);
        var header = JsonConvert.DeserializeObject<ConfigurationHeader>(json);
        if (header == null) continue;

        await conn.ExecuteAsync(
            "INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive], [IsJsonEncrypted]) VALUES (@Id, @Name, @CreatedUtc, @UpdateUtc, @Deleted, @IsActive, @IsJsonEncrypted)",
            new { header.Id, header.Name, header.CreatedUtc, header.UpdateUtc, header.Deleted, header.IsActive, header.IsJsonEncrypted });

        foreach (var config in header.Configurations)
        {
            // Fix empty HeaderId (file provider stores empty string)
            var headerId = string.IsNullOrWhiteSpace(config.HeaderId) ? header.Id : config.HeaderId;

            await conn.ExecuteAsync(
                "INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted], [IsJsonEncrypted]) VALUES (@Id, @HeaderId, @Json, @CreatedUtc, @IsActive, @Deleted, @IsJsonEncrypted)",
                new { config.Id, HeaderId = headerId, config.Json, config.CreatedUtc, config.IsActive, config.Deleted, config.IsJsonEncrypted });

            // Insert junction table rows - only for applications that exist
            foreach (var appId in config.Applications)
            {
                try
                {
                    await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES (@ConfigId, @AppId)", new { ConfigId = config.Id, AppId = appId });
                }
                catch (SqlException) { Console.WriteLine($"    Warning: App {appId} not found for config {config.Id}"); }
            }
            foreach (var envId in config.Environments)
            {
                try
                {
                    await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES (@ConfigId, @EnvId)", new { ConfigId = config.Id, EnvId = envId });
                }
                catch (SqlException) { Console.WriteLine($"    Warning: Env {envId} not found for config {config.Id}"); }
            }
        }
        Console.WriteLine($"  Config: {header.Name} ({header.Id}) - {header.Configurations.Count} version(s)");
    }

    // Migrate Secrets
    var secretDir = Path.Combine(fileDbPath, "secrets");
    var secretFiles = Directory.GetFiles(secretDir, "*.txt").Where(f => !Path.GetFileName(f).StartsWith("AuditLog")).ToList();
    Console.WriteLine($"\nMigrating {secretFiles.Count} secret headers...");
    foreach (var file in secretFiles)
    {
        var json = await File.ReadAllTextAsync(file);
        var header = JsonConvert.DeserializeObject<SecretHeader>(json);
        if (header == null) continue;

        await conn.ExecuteAsync(
            "INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive]) VALUES (@Id, @Name, @CreatedUtc, @UpdateUtc, @Deleted, @IsActive)",
            new { header.Id, header.Name, header.CreatedUtc, header.UpdateUtc, header.Deleted, header.IsActive });

        foreach (var secret in header.Secrets)
        {
            var headerId = string.IsNullOrWhiteSpace(secret.HeaderId) ? header.Id : secret.HeaderId;

            await conn.ExecuteAsync(
                "INSERT INTO [Secrets] ([Id], [HeaderId], [Value], [ValueType], [CreatedUtc], [IsActive], [Deleted]) VALUES (@Id, @HeaderId, @Value, @ValueType, @CreatedUtc, @IsActive, @Deleted)",
                new { secret.Id, HeaderId = headerId, secret.Value, secret.ValueType, secret.CreatedUtc, secret.IsActive, secret.Deleted });

            foreach (var appId in secret.Applications)
            {
                try
                {
                    await conn.ExecuteAsync("INSERT INTO [SecretApplications] ([SecretId], [ApplicationId]) VALUES (@SecretId, @AppId)", new { SecretId = secret.Id, AppId = appId });
                }
                catch (SqlException) { Console.WriteLine($"    Warning: App {appId} not found for secret {secret.Id}"); }
            }
            foreach (var envId in secret.Environments)
            {
                try
                {
                    await conn.ExecuteAsync("INSERT INTO [SecretEnvironments] ([SecretId], [EnvironmentId]) VALUES (@SecretId, @EnvId)", new { SecretId = secret.Id, EnvId = envId });
                }
                catch (SqlException) { Console.WriteLine($"    Warning: Env {envId} not found for secret {secret.Id}"); }
            }
        }
        Console.WriteLine($"  Secret: {header.Name} ({header.Id}) - {header.Secrets.Count} version(s)");
    }

    // Migrate Settings
    var settingsFile = Path.Combine(fileDbPath, "settings", "settings.json");
    if (File.Exists(settingsFile))
    {
        var settings = JsonConvert.DeserializeObject<Settings>(await File.ReadAllTextAsync(settingsFile));
        if (settings != null)
        {
            // The CREATE script already inserts a default row, so update it
            await conn.ExecuteAsync("UPDATE [Settings] SET [EncryptAllJson] = @EncryptAllJson WHERE [Id] = 1", new { settings.EncryptAllJson });
            Console.WriteLine($"\nSettings migrated: EncryptAllJson={settings.EncryptAllJson}");
        }
    }

    // Migrate API Keys (old format: plain strings in Keys array)
    var apiKeysFile = Path.Combine(fileDbPath, "settings", "apikeys.json");
    if (File.Exists(apiKeysFile))
    {
        var json = await File.ReadAllTextAsync(apiKeysFile);
        var obj = JObject.Parse(json);
        var keysArray = obj["Keys"] as JArray;
        if (keysArray != null)
        {
            Console.WriteLine($"\nMigrating {keysArray.Count} API key(s)...");
            var index = 0;
            foreach (var token in keysArray)
            {
                string name, key;
                if (token.Type == JTokenType.String)
                {
                    // Old format: plain string
                    key = token.Value<string>()!;
                    name = string.Empty;
                }
                else
                {
                    // New format: { Name, Key }
                    name = token.Value<string>("Name") ?? string.Empty;
                    key = token.Value<string>("Key") ?? string.Empty;
                }
                await conn.ExecuteAsync("INSERT INTO [ApiKeys] ([Name], [Key]) VALUES (@Name, @Key)", new { Name = name, Key = key });
                Console.WriteLine($"  ApiKey: '{name}' = {key[..Math.Min(8, key.Length)]}...");
                index++;
            }
        }
    }

    // Print summary counts
    Console.WriteLine("\n=== Summary ===");
    Console.WriteLine($"Applications:         {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Applications]")}");
    Console.WriteLine($"Environments:         {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Environments]")}");
    Console.WriteLine($"ConfigurationHeaders: {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationHeaders]")}");
    Console.WriteLine($"Configurations:       {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Configurations]")}");
    Console.WriteLine($"ConfigApps junctions: {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationApplications]")}");
    Console.WriteLine($"ConfigEnvs junctions: {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationEnvironments]")}");
    Console.WriteLine($"SecretHeaders:        {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretHeaders]")}");
    Console.WriteLine($"Secrets:              {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Secrets]")}");
    Console.WriteLine($"SecretApps junctions: {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretApplications]")}");
    Console.WriteLine($"SecretEnvs junctions: {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretEnvironments]")}");
    Console.WriteLine($"ApiKeys:              {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ApiKeys]")}");
    Console.WriteLine($"Settings:             {await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Settings]")}");
}
