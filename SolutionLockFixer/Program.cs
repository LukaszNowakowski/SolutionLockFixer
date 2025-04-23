namespace SolutionLockFixer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    internal static class Program
    {
        private static List<string> listOfSolutionsPaths = new List<string>();

        private static void Main(string[] args)
        {
            var noOfSolutions = Convert.ToInt32(ConfigurationManager.AppSettings["NoOfSolutions"]);
            SetupSolutions(noOfSolutions);

            var isValidInput = false;
            var solutionDirectory = string.Empty;

            while (!isValidInput)
            {
                DisplayConfiguredSolutions(noOfSolutions);
                solutionDirectory = Console.ReadLine();

                if (solutionDirectory == string.Empty)
                {
                    solutionDirectory = "1";
                }

                if (IsInputValueAnExitCode(solutionDirectory))
                {
                    break;
                }

                var isIntInput = int.TryParse(solutionDirectory, out var selectedConfiguredSolution);
                if (isIntInput)
                {
                    if (selectedConfiguredSolution <= 0 ||
                        selectedConfiguredSolution > noOfSolutions)
                    {
                        WriteErrorMessage("Numeric input value '{0}' is outside the configured solutions range", selectedConfiguredSolution);
                        continue;
                    }

                    solutionDirectory = listOfSolutionsPaths[selectedConfiguredSolution - 1];
                }

                if (!Directory.Exists(solutionDirectory))
                {
                    WriteErrorMessage("Directory '{0}' doesn't exist", solutionDirectory);
                    continue;
                }

                isValidInput = true;
            }

            if (IsInputValueAnExitCode(solutionDirectory))
            {
                return;
            }

            var solutionLockFile = Path.Combine(solutionDirectory, "solution.lock.json");

            var slnFile = Directory.GetFiles(solutionDirectory, "*.sln").First();

            var slnContent = File.ReadAllText(slnFile);
            var csprojFiles = slnContent.Split('"').Where(text => text.Contains(".csproj")).ToList();
            var projectDirectories = csprojFiles.Select(file => Path.Combine(solutionDirectory, Path.GetDirectoryName(file))).ToList();

            var projectLockFiles = projectDirectories.SelectMany(projectDir => Directory.GetFiles(projectDir, "project.lock.json")).ToList();

            if (projectLockFiles.Count == 0)
            {
                WriteErrorMessage("No project.json files found");

                Console.WriteLine("Press ENTER to exit");
                Console.ReadLine();
                return;
            }

            var projectFiles = projectLockFiles.Select(ParseFile).ToList();
            var output = new JObject { { "locked", false }, { "version", 3 } };
            var librariesNode = CreateLibrariesNode(projectFiles);
            output.Add("libraries", librariesNode);
            File.WriteAllText(solutionLockFile, output.ToString());
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done. File generated to '{0}'", solutionLockFile);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static void WriteErrorMessage(string errorMessage, params object[] arg)
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage, arg);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
        }

        private static void SetupSolutions(int noOfSolutions)
        {
            for (int i = 0; i < noOfSolutions; i++)
            {
                var pathSetting = ConfigurationManager.AppSettings["SolutionPath_" + (i + 1)];
                listOfSolutionsPaths.Add(pathSetting);
            }
        }

        private static void DisplayConfiguredSolutions(int noOfSolutions)
        {
            Console.WriteLine("Configured Solutions: ");
            Console.WriteLine();
            for (int i = 0; i < noOfSolutions; i++)
            {
                var pathSetting = listOfSolutionsPaths[i];
                Console.WriteLine((i + 1) + ". " + pathSetting);
            }

            Console.WriteLine();
            Console.Write("Solution directory ('1' by default, 'x' to exit): ");
        }

        private static bool IsInputValueAnExitCode(string input)
        {
            return input.ToLower() == "x";
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
