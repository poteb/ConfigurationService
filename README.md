


# ConfigurationService

Application configuration taken to the next level.

- Centralized and shared application configuration.
- Fetch configuration through an API.
- Frontend Blazor app to manage the configurations.

## Overview

The Configuration Service enables applications to:
- Store sensitive configuration values (connection strings, API keys, secrets) outside of source control
- Manage environment-specific configurations centrally
- Use template-based configuration with reference patterns
- Merge local and remote configuration sources seamlessly
- Write fully resolved configuration to parsed output files for debugging

### The reason behind it
I don't like configuration-as-code. I should never be forced to release my application to make a change to a setting. Not even if the changes to settings is done in the release pipeline, i.e. Octopus Deploy or Azure DevOps. I just don't like it.
I prefer a system where I can make the changes on the fly, test them and still have version control. This is what this service can offer.

### Creator's note
I've so far created everything here myself. But I'd be happy to get your (yes you!) view on this.

It can at first be hard to wrap your head around the concepts described below. Here you'll see that configuration isn't static anymore.

## Middleware Installation

Install the NuGet package in your project:

```bash
dotnet add package pote.Configuration.Middleware
```

## Quick Start

### 1. Add the Configuration Service URL to appsettings.json

Create environment-specific appsettings files with the Configuration Service URL:

**appsettings.Development.json:**
```json
{
  "ConfigurationServiceApiUrl": "http://your-config-server:port"
}
```

**appsettings.Production.json:**
```json
{
  "ConfigurationServiceApiUrl": "http://your-config-server:port"
}
```

### 2. Configure in Program.cs

Add the Configuration Service during application startup:

```csharp
using pote.Config.Middleware;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add Configuration Service
AddConfiguration(builder);

var app = builder.Build();
app.Run();

static void AddConfiguration(WebApplicationBuilder builder)
{
    var environmentName = builder.Environment.EnvironmentName;

    try
    {
        // Read the environment-specific appsettings file
        var envAppsettingsContent = File.ReadAllText($"appsettings.{environmentName}.json");

        // Parse to extract the Configuration Service URL
        var appSettingsJsonDoc = JsonDocument.Parse(envAppsettingsContent);
        var configurationApiUrl = appSettingsJsonDoc.RootElement
            .GetProperty("ConfigurationServiceApiUrl")
            .GetString() ?? throw new InvalidOperationException("ConfigurationServiceApiUrl not found");

        // Configure the Configuration Service client
        var configSettings = new BuilderConfiguration
        {
            Application = "YourApplicationName",
            Environment = environmentName,
            WorkingDirectory = "",
            RootApiUri = configurationApiUrl
        };

        // Fetch and merge configuration from the service
        _ = builder.Configuration
            .AddConfigurationFromApi(
                configSettings,
                envAppsettingsContent,
                () => new HttpClient(),
                (message, ex) => Console.WriteLine($"Error: {message}", ex))
            .Result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading app settings. Environment: {environmentName}", ex);
        throw;
    }
}
```

### 3. Alternative: Worker Service Configuration

For background services or workers, use the `ConfigureAppConfiguration` method:

```csharp
var builder = Host.CreateApplicationBuilder(args);
var environmentName = builder.Environment.EnvironmentName;
var folder = AppContext.BaseDirectory;

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Configuration.AddJsonFile($"appsettings.{environmentName}.json", optional: false);

builder.Configuration.ConfigureAppConfiguration((hostingContext, config) =>
{
    var envAppsettingsContent = File.ReadAllText($"appsettings.{environmentName}.json");
    var appSettingsJsonDoc = JsonDocument.Parse(envAppsettingsContent);
    var configurationApiUrl = appSettingsJsonDoc.RootElement
        .GetProperty("ConfigurationServiceApiUrl")
        .GetString() ?? "unknown";

    var configSettings = new BuilderConfiguration
    {
        Application = "YourServiceName",
        Environment = environmentName,
        WorkingDirectory = folder,
        RootApiUri = configurationApiUrl
    };

    _ = config.AddConfigurationFromApi(
        configSettings,
        envAppsettingsContent,
        () => new HttpClient(),
        (s, ex) => Console.WriteLine(s, ex))
    .Result;
});

var host = builder.Build();
host.Run();
```

## Reusability

It's all about reusability and having a great overview of your application configuration.

The configurations are split into smaller parts and linked together. This makes it possible to reuse the same configuration across multiple applications. It also makes it easier to manage the configurations.

![Basic usage](/documentation/BasicOverview.png)

Example with JSON:

![Example with JSON](/documentation/JsonFlow.png)

## JSON Reference ($ref)
The configurations are stored as JSON files. The JSON reference syntax is used to link the configurations together. 
The reference syntax is `$ref:<path>#<json-property>`. The path is relative to the current configuration file. The json-property is optional and if omitted the entire content of the referenced configuration is used.

Let's look at an example.
Our application *Super Goofy's Super Goobers* has a standard configuration that needs a SQL Server connection string and some RabbitMQ server settings:

##### *Base-configuration (v1):*
    {
      "ConnectionStrings": {
        "Default": "Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword"
      },
      "RabbitMQ": {
        "Server":"MyMqServer",
        "Port":5672,
        "Username":"Goofy",
        "Password":"EpicHeroPassword"
      }
    }
Other applications might need a connection to the same database, and since we hate repeating ourselves (DRY), we move the SQL Server connection string to it's own configuration and reference it from the base-configuration:
##### *ConnectionStrings:*
    {
      "DefaultConnection":"Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
##### *Base-configuration (v2):*
    {
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#DefaultConnection"
      },
      "RabbitMQ": {
        "Server":"MyMqServer",
        "Port":5672,
        "Username":"Goofy",
        "Password":"EpicHeroPassword"
      }
    }

*$ref:ConnectionStrings#Default* is a JSON reference pointing to ConnectionStrings configuration's Default value. When an application is requesting Base-Configuration from the API, the engine will see this reference, pull the content from ConnectionStrings.Default and replace the reference with the value. The returned output looks like Base-configuration v1.

You now know the basics of how this works. But we can go a bit deeper with the RabbitMQ settings.
Just like the SQL connection we can move the entire block to its own configuration.
##### *RabbitMQ:*
    {
      "Server": "MyMqServer",
      "Port": 5672,
      "Username": "Goofy",
      "Password": "EpicHeroPassword"
    }
##### *Base-configuration (v3):*
    {
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#DefaultConnection"
      },
      "RabbitMQ": "$ref:RabbitMQ#"
    }

*$ref:RabbitMQ#* points to the RabbitMQ configuration, but notice that there is nothing after the hash (#). This takes the entire content of the RabbitMQ configuration instead of just the value of the json property. Again the output looks like Base-configuration v1.
But we can to even better. What if each application has it's own login? Let's move the username and password to another configuration.
##### *RabbitMQ-Login:*
    {
      "Username":"Goofy",
      "Password":"EpicHeroPassword"
    }
##### *RabbitMQ:*
    {
      "Server": "MyMqServer",
      "Port": 5672,
      "Username": "$ref:RabbitMQ-Login#Username",
      "Password": "$ref:RabbitMQ-Login#Password"
    }
But as it is now, every application will get the same configuration output because there's nothing that separates an application from another. Each application need to have it's own *RabbitMQ-Login* configuration, but they have to have the same name for the reference from *RabbitMQ* to work. This is done by saving multiple *RabbitMQ-Login* configurations but connected to different applications.

*Note: Each application must be added to a list of application via the admin page.*

First we connect the existing *RabbitMQ-Login* to our application.
##### *RabbitMQ-Login • Application: Super Goofy's Super Goobers:*
    (application: Super Goofy's Super Goobers)
    {
      "Username":"Goofy",
      "Password":"EpicHeroPassword"
    }
It looks the same, but it's connected to our application.
Here's a *RabbitMQ-Login* for another application:
##### *RabbitMQ-Login • Application: More Gold for Scrooge:*
    (application: More Gold for Scrooge)
    {
      "Username":"Scrooge",
      "Password":"LuckyCoin"
    }
Both application use the same base configuration, but the results differ.
##### *Base-configuration • Application: Super Goofy's Super Goobers:*
    {
      "ConnectionStrings": {
        "Default": "Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword"
      },
      "RabbitMQ": {
        "Server":"MyMqServer",
        "Port":5672,
        "Username":"Goofy",
        "Password":"EpicHeroPassword"
      }
    }

##### *Base-configuration • Application: More Gold for Scrooge:*
    {
      "ConnectionStrings": {
        "Default": "Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword"
      },
      "RabbitMQ": {
        "Server":"MyMqServer",
        "Port":5672,
        "Username":"Scrooge",
        "Password":"LuckyCoin"
      }
    }
The only difference is the username and password for RabbitMQ. If we later create another application that has a similar configuration we only need to create a new *RabbitMQ-Login* configuration and link it to our application. If our new application needs the AnotherConnection SQL connection string we just reference that instead, or create a third one.

Besides different applications it's also required to link all configurations to an environment. This makes it possible to use the same set of configurations across different environments. It could be different test environments and development. An example could be *ConnectionStrings* where we want to use a different Default connection string when debugging our application.
##### *ConnectionStrings • Environments: Test1:*
    {
      "DefaultConnection":"Data Source=dbserver;Initial Catalog=myDbTest1;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
##### *ConnectionStrings • Environments: Test2:*
    {
      "DefaultConnection":"Data Source=dbserver;Initial Catalog=myDbTest2;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
##### *ConnectionStrings • Environments: Development:*
    {
      "DefaultConnection":"Data Source=.;Initial Catalog=myDb;Trusted_Connection=True",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
Here we use the same SQL Server instance for our test environments but different databases. And locally we use "." and a trusted connection. The base configuration remains unchanged.

### "Base" naming convention
If the name of the json property being processed is named *base* or *Base* the result of the reference replaces the whole key-value pair.
Let's look at a standard appsettings.json file.
##### *appsettings.json:*

    {
      "ConnectionStrings": {
        "Default": "Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword"
      },
      "RabbitMQ": {
        "Server": "MyMqServer",
        "Port": 5672,
        "Username": "Goofy",
        "Password": "EpicHeroPassword"
      }
    }

We could change this to

    {
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#DefaultConnection"
      },
      "RabbitMQ": "$ref:RabbitMQ#"
    }

But instead we use a base-reference

    {
      "base":"$ref:base-configuration"
    }

which results in 

    {
      "ConnectionStrings": {
        "Default": "Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword"
      },
      "RabbitMQ": {
        "Server": "MyMqServer",~~~~
        "Port": 5672,
        "Username": "Goofy",
        "Password": "EpicHeroPassword"
      }
    }
The *base* key is gone from the original *appsettings.json* and replaced with the result of the reference.
This works because the name of the json property is *base* or *Base*.

## Key vault
If you have sensitive information that you don't want to store in the configuration files you can use a key vault. The secrets are stored as key/value pairs.

A source generator is used to generate the needed code to fetch the secrets from the key vault. The source generator is triggered by the `SecretAttribute`.

Example of a key vault reference in a configuration file:
    
    public partial class MyConfiguration
    {
        [Secret]
        private string _secret1 = "";
    }

The class must be partial.

The generated code will look like this:

    public partial class MyConfiguration : ISecretSettings
    {
        private bool _isSecret1Resolved;
        public string Secret1
        {
            get 
            {
                if (!this._isSecret1Resolved)
                {
                    this._secret1 = SecretResolver.ResolveSecret(this._secret1);
                }
                this._isSecret1Resolved = true;
                return this._secret1;
            }
            set => this._secret1 = value;
        }

        public ISecretResolver SecretResolver { get; set; }
    }

The `ISecretResolver` is an interface that is used to resolve the secrets. The `SecretResolver` is a class that implements the `ISecretResolver` interface. The `SecretResolver` is injected into the `MySecrets` class and used to resolve the secrets.

The secrets are resolved once and only once. This might change in the future to not keep secrets in memory. But a change like that will put a tighter coupling to the configuration service. 

## Admin UI
![Basic usage](/documentation/AdminUi1.png)
Here is an example of a configuration in the Admin UI.

A configuration has a name which is also it's key. It's used to reference the configuration from other configurations. A configuration can have multiple sections, where each section is for one or more environment and applications.

The blue box shows the configuration for the *Development* environment and several applications.

The green box shows the configuration for the *Test02* environment and the same applications.

There is not reference in this example, but it's referenced by another configuration, the *Base-DeliveryNotesApi* configuration. You can see this to the right of the name field.

## Storage
The underlying storage is abstracted away from the JSON parser and admin API, which means it's relatively easy to change it. For now you'll find a working file storage data provider and an incomplete SQL Server data provider. Changing the file location is done in *Config.Api/appsettings.Development.json* and *Config.Admin.Api/appsettings.Development.json*.

## Middleware
*Config.Middleware* contains an extension method for calling and adding the generated configuration to .NET's configuration builder. `IConfigurationBuilder` if .NET Standard and `WebApplicationBuilder` for .NET 6 applications.

If the call to the API fails it will try to load a previously generated configuration file instead. If this fails a `FileNotFoundException` is thrown.

## Security
None. So don't expose the APIs outside your network.

There are two issues that will add api-key security. [#125](https://github.com/poteb/ConfigurationService/issues/125) and [#126](https://github.com/poteb/ConfigurationService/issues/126).

## Build
There is no build pipeline set up for this repository. This is because it's not just a nuget package but multiple full-blown applications.
