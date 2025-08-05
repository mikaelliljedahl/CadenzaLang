using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Cadenza.Tests.Framework
{
    [TestFixture]
    public class TestDiscoveryTests
    {
        [Test]
        public void Discovery_AllTestClassesHaveProperSetup()
        {
            var testClasses = DiscoverTestClasses();
            var missingSetup = new List<string>();

            foreach (var testClass in testClasses)
            {
                var setupMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttributes<SetUpAttribute>().Any())
                    .ToList();

                if (!setupMethods.Any())
                {
                    missingSetup.Add(testClass.Name);
                }
            }

            if (missingSetup.Any())
            {
                TestContext.WriteLine($"Warning: Test classes without [SetUp] methods: {string.Join(", ", missingSetup)}");
            }
        }

        [Test]
        public void Discovery_AllTestMethodsAreProperlyNamed()
        {
            var testClasses = DiscoverTestClasses();
            var badlyNamedTests = new List<string>();

            foreach (var testClass in testClasses)
            {
                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any())
                    .ToList();

                foreach (var method in testMethods)
                {
                    if (!IsWellNamedTestMethod(method.Name))
                    {
                        badlyNamedTests.Add($"{testClass.Name}.{method.Name}");
                    }
                }
            }

            Assert.That(badlyNamedTests, Is.Empty, 
                $"Poorly named test methods found: {string.Join(", ", badlyNamedTests)}");
        }

        [Test]
        public void Discovery_GenerateTestReport()
        {
            var report = GenerateTestReport();
            
            TestContext.WriteLine("=== Test Discovery Report ===");
            TestContext.WriteLine($"Total Test Classes: {report.TotalTestClasses}");
            TestContext.WriteLine($"Total Test Methods: {report.TotalTestMethods}");
            TestContext.WriteLine($"Unit Tests: {report.UnitTests}");
            TestContext.WriteLine($"Integration Tests: {report.IntegrationTests}");
            TestContext.WriteLine($"Golden File Tests: {report.GoldenFileTests}");
            TestContext.WriteLine($"Performance Tests: {report.PerformanceTests}");
            TestContext.WriteLine($"Regression Tests: {report.RegressionTests}");
            
            TestContext.WriteLine("\n=== Test Coverage by Component ===");
            foreach (var component in report.ComponentCoverage)
            {
                TestContext.WriteLine($"{component.Key}: {component.Value} tests");
            }

            // Ensure we have comprehensive coverage
            Assert.That(report.UnitTests, Is.GreaterThan(0), "No unit tests found");
            Assert.That(report.IntegrationTests, Is.GreaterThan(0), "No integration tests found");
            Assert.That(report.GoldenFileTests, Is.GreaterThan(0), "No golden file tests found");
        }

        [Test]
        public void Discovery_ValidateTestOrganization()
        {
            var testAssembly = Assembly.GetExecutingAssembly();
            var testClasses = testAssembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestFixtureAttribute>().Any())
                .ToList();

            var organizationIssues = new List<string>();

            foreach (var testClass in testClasses)
            {
                // Check namespace organization
                var namespaceParts = testClass.Namespace?.Split('.') ?? new string[0];
                if (namespaceParts.Length < 3) // Cadenza.Tests.{Category}
                {
                    organizationIssues.Add($"{testClass.Name}: Improper namespace organization");
                }

                // Check class naming conventions
                if (!testClass.Name.EndsWith("Tests"))
                {
                    organizationIssues.Add($"{testClass.Name}: Should end with 'Tests'");
                }
            }

            Assert.That(organizationIssues, Is.Empty, 
                $"Test organization issues: {string.Join(", ", organizationIssues)}");
        }

        private List<Type> DiscoverTestClasses()
        {
            var testAssembly = Assembly.GetExecutingAssembly();
            return testAssembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestFixtureAttribute>().Any())
                .ToList();
        }

        private bool IsWellNamedTestMethod(string methodName)
        {
            // Test methods should follow pattern: Component_ShouldBehavior or Component_ShouldBehavior_WhenCondition
            var validPrefixes = new[] { 
                "Lexer_", "Parser_", "CodeGenerator_", "Transpiler_", "GoldenFile_", "Regression_", "Discovery_",
                "Ast_", "Tokens_", "PackageManager_", "Analysis_", "Reporting_", "PackageIntegration_", "ProjectCompiler_"
            };
            
            if (!validPrefixes.Any(prefix => methodName.StartsWith(prefix)))
                return false;

            // Should contain "Should" or be a framework method
            if (!methodName.Contains("Should") && !methodName.StartsWith("Discovery_") && !methodName.StartsWith("Regression_"))
                return false;

            return true;
        }

        private TestReport GenerateTestReport()
        {
            var testClasses = DiscoverTestClasses();
            var report = new TestReport();

            foreach (var testClass in testClasses)
            {
                report.TotalTestClasses++;

                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any())
                    .ToList();

                report.TotalTestMethods += testMethods.Count;

                // Categorize tests by namespace
                var namespaceParts = testClass.Namespace?.Split('.') ?? new string[0];
                if (namespaceParts.Length >= 3)
                {
                    var category = namespaceParts[2]; // Cadenza.Tests.{Category}
                    switch (category.ToLower())
                    {
                        case "unit":
                            report.UnitTests += testMethods.Count;
                            break;
                        case "integration":
                            report.IntegrationTests += testMethods.Count;
                            break;
                        case "golden":
                            report.GoldenFileTests += testMethods.Count;
                            break;
                        case "performance":
                            report.PerformanceTests += testMethods.Count;
                            break;
                        case "regression":
                            report.RegressionTests += testMethods.Count;
                            break;
                    }
                }

                // Analyze component coverage
                foreach (var method in testMethods)
                {
                    var component = ExtractComponentFromTestName(method.Name);
                    if (!string.IsNullOrEmpty(component))
                    {
                        if (!report.ComponentCoverage.ContainsKey(component))
                        {
                            report.ComponentCoverage[component] = 0;
                        }
                        report.ComponentCoverage[component]++;
                    }
                }
            }

            return report;
        }

        private string ExtractComponentFromTestName(string testName)
        {
            var parts = testName.Split('_');
            return parts.Length > 0 ? parts[0] : "";
        }
    }

    public class TestReport
    {
        public int TotalTestClasses { get; set; }
        public int TotalTestMethods { get; set; }
        public int UnitTests { get; set; }
        public int IntegrationTests { get; set; }
        public int GoldenFileTests { get; set; }
        public int PerformanceTests { get; set; }
        public int RegressionTests { get; set; }
        public Dictionary<string, int> ComponentCoverage { get; set; } = new();
    }

    public static class TestDataManager
    {
        private static readonly string TestDataPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", 
            "testdata"
        );

        public static void EnsureTestDataExists()
        {
            if (!Directory.Exists(TestDataPath))
            {
                Directory.CreateDirectory(TestDataPath);
            }

            // Create sample test data if it doesn't exist
            CreateSampleCadenzaFiles();
        }

        public static string GetTestDataPath(string fileName)
        {
            EnsureTestDataExists();
            return Path.Combine(TestDataPath, fileName);
        }

        public static void SaveTestResult(string testName, object result)
        {
            var resultPath = Path.Combine(TestDataPath, $"{testName}_result.json");
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(resultPath, json);
        }

        public static T LoadTestResult<T>(string testName)
        {
            var resultPath = Path.Combine(TestDataPath, $"{testName}_result.json");
            if (!File.Exists(resultPath))
                throw new FileNotFoundException($"Test result file not found: {resultPath}");

            var json = File.ReadAllText(resultPath);
            return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize test result");
        }

        private static void CreateSampleCadenzaFiles()
        {
            var samples = new Dictionary<string, string>
            {
                ["simple.cdz"] = "function add(a: int, b: int) -> int { return a + b }",
                ["complex.cdz"] = @"
                    function processData(input: string) -> Result<int, string> {
                        let parsed = parseInt(input)?
                        if parsed < 0 {
                            return Error(""Value must be positive"")
                        }
                        return Ok(parsed * 2)
                    }",
                ["module.cdz"] = @"
                    module Utils {
                        pure function square(x: int) -> int {
                            return x * x
                        }
                        export {square}
                    }"
            };

            foreach (var sample in samples)
            {
                var filePath = GetTestDataPath(sample.Key);
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, sample.Value);
                }
            }
        }
    }
}