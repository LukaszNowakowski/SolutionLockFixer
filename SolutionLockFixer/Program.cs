namespace SolutionLockFixer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Solution directory: ");
            var solutionDirectory = Console.ReadLine();
            if (!Directory.Exists(solutionDirectory))
            {
                Console.WriteLine("Directory '{0}' doesn't exist", solutionDirectory);
                return;
            }

            var solutionLockFile = Path.Combine(solutionDirectory, "solution.lock.json");
            if (!File.Exists(solutionLockFile))
            {
                Console.WriteLine("solution.lock.json doesn't exist");
                return;
            }

            var projectLockFiles = Directory.GetFiles(solutionDirectory, "project.lock.json", SearchOption.AllDirectories);
            if (projectLockFiles.Length == 0)
            {
                Console.WriteLine("No project.json files found");
                return;
            }

            var projectFiles = projectLockFiles.Select(ParseFile);
            var allDependencies = projectFiles.SelectMany(ExtractDependencies)
                .Distinct(new DependenciesEqualityComparer())
                .OrderBy(t => t.Path);
            var output = new JObject { { "locked", "false" }, { "version", "3" } };
            var librariesNode = new JObject();
            foreach (var dependency in allDependencies)
            {
                librariesNode.Add(dependency);
            }

            output.Add("libraries", librariesNode);
            var projectNode = new JObject();
            var outputPath = Path.Combine(solutionDirectory, "solution.lock.json");
            File.WriteAllText(outputPath, output.ToString());
            Console.ReadLine();
        }

        private static JObject ParseFile(string fullPath)
        {
            return JObject.Parse(File.ReadAllText(fullPath));
        }

        private static IEnumerable<JProperty> ExtractDependencies(JObject projectFile)
        {
            var libs = projectFile["libraries"];
            return libs.Children()
                .OfType<JProperty>();
        }
    }
}
