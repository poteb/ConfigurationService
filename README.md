


# ConfigurationService

Application configuration taken to the next level.

- Centralized and shared application configuration.
- Fetch configuration through an API.
- Frontend Blazor app to manage the configurations.

### The reason behind it
I don't like configuration-as-code. I should never be forced to release my application to make a change to a setting. Not even if the changes to settings is done in the release pipeline, i.e. Octopus Deploy or Azure DevOps. I just don't like it.
I prefer a system where I can make the changes on the fly, test them and still have version control. This is what this service can offer.

### Creator's note
I've so far created everything here myself. But I'd be happy to get your (yes you!) view on this.

It can at first be hard to wrap your head around the concepts described below. Here you'll see that configuration isn't static anymore.

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

Besides different applications it's also required to link all configurations to an environment. This makes it possible to use the same set of configurations across different environments. It could be different test environments and development. An example could *ConnectionStrings* where we want to use a different Default connection string when debugging our application.
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
Here we use different the same SQL Server instance for our test environments but different databases. And locally we use "." and a trusted connection. The base configuration remains unchanged.

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
        "Server": "MyMqServer",
        "Port": 5672,
        "Username": "Goofy",
        "Password": "EpicHeroPassword"
      }
    }
The *base* key is gone from the original *appsettings.json* and replaced with the result of the reference.
This works because the name of the json property is *base* or *Base*.

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
