using Config.Middleware.TestApi.Nuget;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using pote.Config.Middleware.Secrets;

namespace Config.Middleware.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        // Arrange
        var typeToTest = typeof(MySecrets);  // Replace with the type you want to test
        
        // Get the source file path - this assumes your test class is in the same folder as the test
        var sourceFilePath = Path.Combine(@"..\..\..\..\Config.Middleware.TestApi.Nuget\MySecrets.cs");//,  // Navigate up from bin/Debug/net6.0
            //$"{typeToTest.Name}.cs");

        // Read the actual source file
        var sourceText = File.ReadAllText(sourceFilePath);

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create an instance of your generator
        var generator = new SecretSettingsSourceGenerator();  // Replace with your actual generator class
        
        // Create a driver for testing the generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        // Run the generator
        driver = driver.RunGenerators(compilation);

        // Get the results
        var runResult = driver.GetRunResult();

        // Access the syntax receiver from your generator's results
        //var syntaxReceiver = runResult.Results[0].Generator.SyntaxReceiver as SecretSettingsSourceGenerator;
        
        // Assert
        //Assert.NotNull(syntaxReceiver);
    }
}