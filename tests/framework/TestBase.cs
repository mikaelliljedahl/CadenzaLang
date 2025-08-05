using System;
using System.IO;
using NUnit.Framework;

namespace Cadenza.Tests.Framework
{
    /// <summary>
    /// Base class for all Cadenza tests that provides proper working directory context
    /// and cleanup functionality.
    /// </summary>
    public abstract class TestBase
    {
        protected string ProjectRoot { get; private set; }
        protected string OriginalWorkingDirectory { get; private set; }
        protected string TestTempDirectory { get; private set; }
        
        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Store original working directory
            OriginalWorkingDirectory = Directory.GetCurrentDirectory();
            
            // Find project root (directory containing .git or cadenzac.json)
            ProjectRoot = FindProjectRoot();
            
            // Ensure we're working from project root for consistent relative paths
            Directory.SetCurrentDirectory(ProjectRoot);
        }
        
        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            // Restore original working directory
            if (!string.IsNullOrEmpty(OriginalWorkingDirectory))
            {
                Directory.SetCurrentDirectory(OriginalWorkingDirectory);
            }
        }
        
        [SetUp]
        public virtual void SetUp()
        {
            // Create unique temp directory for each test
            TestTempDirectory = Path.Combine(Path.GetTempPath(), "cadenza_tests_" + Guid.NewGuid());
            Directory.CreateDirectory(TestTempDirectory);
            
            // Ensure we're in project root
            Directory.SetCurrentDirectory(ProjectRoot);
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            // Clean up temp directory
            if (Directory.Exists(TestTempDirectory))
            {
                try
                {
                    Directory.Delete(TestTempDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            // Restore to project root
            Directory.SetCurrentDirectory(ProjectRoot);
        }
        
        private string FindProjectRoot()
        {
            var current = Directory.GetCurrentDirectory();
            
            while (current != null)
            {
                // Look for git repository or cadenzac.json
                if (Directory.Exists(Path.Combine(current, ".git")) ||
                    File.Exists(Path.Combine(current, "cadenzac.json")) ||
                    File.Exists(Path.Combine(current, "CLAUDE.md")))
                {
                    return current;
                }
                
                var parent = Directory.GetParent(current);
                if (parent == null)
                    break;
                    
                current = parent.FullName;
            }
            
            // Fallback to current directory
            return Directory.GetCurrentDirectory();
        }
        
        /// <summary>
        /// Gets the absolute path to a file in the project root
        /// </summary>
        protected string GetProjectPath(string relativePath)
        {
            return Path.Combine(ProjectRoot, relativePath);
        }
        
        /// <summary>
        /// Gets the absolute path to a file in the test temp directory
        /// </summary>
        protected string GetTempPath(string relativePath)
        {
            return Path.Combine(TestTempDirectory, relativePath);
        }
        
        /// <summary>
        /// Creates a temporary file with the specified content
        /// </summary>
        protected string CreateTempFile(string fileName, string content)
        {
            var filePath = GetTempPath(fileName);
            var directory = Path.GetDirectoryName(filePath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, content);
            return filePath;
        }
        
        /// <summary>
        /// Copies a file from the project to the temp directory
        /// </summary>
        protected string CopyProjectFileToTemp(string relativePath)
        {
            var sourcePath = GetProjectPath(relativePath);
            var destPath = GetTempPath(relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            
            File.Copy(sourcePath, destPath, true);
            return destPath;
        }
    }
}