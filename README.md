
# ConfigurationService

Centralize application-configuration and fetch through an API.
Frontend Blazor app to manage the configurations.

## But wait! There's more!
Administering the configurations is now a whole lot easier with the power of JSON ref.
Let's look an example.
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
##### *Base-configuration (v2):*
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
##### *RabbitMQ-Login • More Gold for Scrooge:*
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

##### *Base-configuration • More Gold for Scrooge:*
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
