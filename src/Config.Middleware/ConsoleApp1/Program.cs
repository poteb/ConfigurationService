// This app is for debugging the generator. https://github.com/JoanComasFdz/dotnet-how-to-debug-source-generator-vs2022
// Set Config.Middleware.Secrets as the startup project

using ConsoleApp1;
using pote.Config.Middleware;

Console.WriteLine("Hello, World!");

var ms = new MySecrets();
Console.WriteLine(nameof(ms.Secret));

Console.ReadLine();