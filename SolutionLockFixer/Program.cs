namespace SolutionLockFixer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.Write("Solution directory: ");
            var solutionDirectory = Console.ReadLine();
            if (!Directory.Exists(solutionDirectory))
            {
                Console.WriteLine("Directory '{0}' doesn't exist", solutionDirectory);
                return;
            }

            var solutionLockFile = Path.Combine(solutionDirectory, "solution.lock.json");
            var projectLockFiles = Directory.GetFiles(solutionDirectory, "project.lock.json", SearchOption.AllDirectories);
            if (projectLockFiles.Length == 0)
            {
                Console.WriteLine("No project.json files found");
                return;
            }

            var projectFiles = projectLockFiles.Select(ParseFile).ToList();
            var output = new JObject { { "locked", false }, { "version", 3 } };
            var librariesNode = CreateLibrariesNode(projectFiles);
            output.Add("libraries", librariesNode);
            File.WriteAllText(solutionLockFile, output.ToString());
            Console.WriteLine("Done. File generated to '{0}'", solutionLockFile);
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static JObject CreateLibrariesNode(IEnumerable<JObject> projectFiles)
        {
            var librariesNode = new JObject();
            var allDependencies = projectFiles.SelectMany(ExtractDependencies)
                .Where(p => (p.Value["type"] as JValue)?.Value?.ToString() != "project")
                .Distinct(new DependenciesEqualityComparer())
                .OrderBy(t => t.Path);
            foreach (var dependency in allDependencies)
            {
                librariesNode.Add(dependency);
            }

            return librariesNode;
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
