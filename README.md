

# ConfigurationService

Centralize application-configuration and fetch through an API.
Frontend Blazor app to manage the configurations.

### Creator's note, one-man-mad-idea
I've so far created everything here myself. But I'd be happy to get your (yes you!) view on this.

It can at first be hard to wrap your head around the concepts described below. Here you'll see that configuration isn't static anymore. IT'S ALIVE!

## But wait! There's more!
Administering the configurations is now a whole lot easier with the power of JSON ref.
Let's look an example.
Our application *Super Goofy's Super Goobers* has a standard configuration that needs a SQL Server connection string and some RabbitMQ server settings:

##### *Base-configuration (v1):*
    {~~~~
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

##### *Base-configuration (v2):*
    {
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#Default"
      },
      "RabbitMQ": {
        "Server":"MyMqServer",
        "Port":5672,
        "Username":"Goofy",
        "Password":"EpicHeroPassword"
      }
    }

##### *ConnectionStrings:*
    {
      "Default":"Data Source=dbserver;Initial Catalog=myDb;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
*$ref:ConnectionStrings#Default* is a JSON reference pointing to ConnectionStrings configuration's Default value. The output looks like Base-configuration v1.
Now let's look at the RabbitMQ settings. Just like the SQL connection we can move the entire block to its own configuration.
##### *Base-configuration (v3):*
    {
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#Default"
      },
      "RabbitMQ": "$ref:RabbitMQ#"
    }
##### *RabbitMQ:*
    {
      "Server": "MyMqServer",
      "Port": 5672,
      "Username": "Goofy",
      "Password": "EpicHeroPassword"
    }
*$ref:RabbitMQ#* points to the RabbitMQ configuration, but here there's nothing after the hash (#). This takes the entire content of the RabbitMQ configuration instead of just a value. Again the output looks like Base-configuration v1.
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
But as it is now, every application will get the same configuration output because there's nothing that separates an application from another. Each application need to have it's own *RabbitMQ-Login*, but they have to have the same name for the reference from *RabbitMQ* to work. This is done by saving multiple *RabbitMQ-Login* configurations but connected to different applications (systems).
First we connect the existing *RabbitMQ-Login* to our application.
##### *RabbitMQ-Login • System: Super Goofy's Super Goobers:*
    {
      "Username":"Goofy",
      "Password":"EpicHeroPassword"
    }
It looks the same, but it's connected to our application.
Here's a *RabbitMQ-Login* for another application:
##### *RabbitMQ-Login • System: More Gold for Scrooge:*
    {
      "Username":"Scrooge",
      "Password":"LuckyCoin"
    }
Both application use the same base configuration, but the results differ.
##### *Base-configuration • System: Super Goofy's Super Goobers:*
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

##### *Base-configuration • System: More Gold for Scrooge:*
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

Besides different systems it's also required to link all configurations to an environment. This makes it possible to use the same set of configurations across different environments. It could be different test environments and development. An example could *ConnectionStrings* where we want to use a different Default connection string when debugging our application.
##### *ConnectionStrings • Environments: Test1:*
    {
      "Default":"Data Source=dbserver;Initial Catalog=myDbTest1;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
##### *ConnectionStrings • Environments: Test2:*
    {
      "Default":"Data Source=dbserver;Initial Catalog=myDbTest2;User Id=sa;Password=SuperNinjaPassword",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
##### *ConnectionStrings • Environments: Development:*
    {
      "Default":"Data Source=.;Initial Catalog=myDb;Trusted_Connection=True",
      "AnotherConnection":"Server=dbserver;Database=yourDb;Trusted_Connection=True"
    }
Here we use different the same SQL Server instance for our test environments but different databases. And locally we use "." and a trusted connection. The base configuration remains unchanged.

### "Base" naming convention
If the name of the json property being processed is named *base* or *Base* the result of the reference replaces the whole key-value pair.
Let's look at a standard appsettings.json file.
##### *appsettings.json:*

    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
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
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "ConnectionStrings": {
        "Default": "$ref:ConnectionStrings#Default"
      },
      "RabbitMQ": "$ref:RabbitMQ#"
    }

But instead we use a base-reference

    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "base":"$ref:base-configuration"
    }

which results in 

    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
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
The *base* key is gone from the original *appsettings.json* and replaced with the result of the reference.
This works because the name of the json property is *base* or *Base*.

## Storage
The underlying storage is abstracted away from the JSON parser and admin API, which means it's relatively easy to change it. For now you'll find a working file storage data provider and an incomplete SQL Server data provider. Changing the file location is done in *Config.Api/appsettings.Development.json* and *Config.Admin.Api/appsettings.Development.json*.

## Middleware
*Config.Middleware* contains an extension method for calling and adding the generated configuration to .NET's configuration builder. `IConfigurationBuilder` if .NET Standard and `WebApplicationBuilder` for .NET 6 applications.

If the call to the API fails it will try to load a previously generated configuration file instead. If this fails a `FileNotFoundException` is thrown.~~~~

## Security
None. So don't expose the endpoints externally.