// See https://aka.ms/new-console-template for more information

using ConsoleApp1;
using pote.Config.Middleware;

Console.WriteLine("Hello, World!");

var ms = new MySecrets();
Console.WriteLine(nameof(ms.Secret));

Console.ReadLine();