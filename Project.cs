using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace DependencyAnalyser
{
    public class Project : IDependency
    {
        private static readonly Regex dllDependencyRegex = new Regex("\\<Reference\\s*Include=\\\"(?<dllname>[^,\"]*)[^\"]*\\\"");
        private static readonly Regex projectDependencyRegex = new Regex("\\<ProjectReference\\s*Include=\\\"(?<projectpath>.*)\\\\(?<csproj>[^\\\\\"]*)\\.csproj\\\"");
        private static readonly Regex assemblyNameRegex = new Regex("\\<AssemblyName\\>(?<assemblyname>[^\\<]*)\\</AssemblyName\\>");

        private string File { get; set; }
        private string Dir { get; set; }

        public string Name { get; set; }
        public string AssemblyName { get; set; }
        public HashSet<Solution> Components { get; set; }

        private readonly List<Project> dependentProjects = new List<Project>();
        private readonly List<Project> dependingProjects = new List<Project>();

        public List<Project> DependentProjects
        {
            get { return dependentProjects; }
        }

        public List<Project> DependingProjects
        {
            get { return dependingProjects; }
        }

        public Project(string file, string dir, string name)
        {
            File = file;
            Dir = dir;
            Name = name;
            Components = new HashSet<Solution>();
        }

        public override string ToString()
        {
            return string.Format("Name: {0}", Name);
        }

        public void CalculateDependencies(Projects allProjects, IDictionary<string, string> dllNameToProjectName)
        {
            using (StreamReader file = OpenFile())
            {
                string line;
                while( (line = file.ReadLine()) != null)
                {
                    if (line.Contains("Test")) continue;
                    HandleProjectDependencies(line, allProjects);
                    HandleDllDependencies(line, allProjects, dllNameToProjectName);
                }
            }
            dependentProjects.Sort((first,second) => first.Name.CompareTo(second.Name));
        }

        private void HandleProjectDependencies(string line, Projects allProjects)
        {
            if (!projectDependencyRegex.IsMatch(line))
            {
                SanityCheckMissingMatches(line, "ProjectReference");
                return;
            }
            AddDependency(projectDependencyRegex.Match(line).Value("csproj"), allProjects);
        }

        private static void SanityCheckMissingMatches(string line, string xmlElement)
        {
            if (line.Contains("<"+xmlElement)) Console.Error.WriteLine("Sanity: " + line);
        }

        private void HandleDllDependencies(string line, Projects allProjects, IDictionary<string, string> dllNameToProjectName)
        {
            if (!dllDependencyRegex.IsMatch(line))
            {
                SanityCheckMissingMatches(line, "Reference");
                return;
            }
            AddDependency(ResolveDependencyName(dllDependencyRegex.Match(line).Value("dllname"), dllNameToProjectName), allProjects);
        }

        private static string ResolveDependencyName(string dllName, IDictionary<string, string> allProjectsByAssemblyName)
        {
            return allProjectsByAssemblyName.ContainsKey(dllName) ? allProjectsByAssemblyName[dllName] : dllName;
        }

        private void AddDependency(string dependencyName, Projects allProjects)
        {
            if (!allProjects.ContainsKey(dependencyName))
            {
                allProjects.AddIgnoredDependency(dependencyName);
                return;
            }
            AddDependency(allProjects[dependencyName]);
        }

        private void AddDependency(Project dependsOn)
        {
            dependentProjects.Add(dependsOn);
            dependsOn.dependingProjects.Add(this);
        }

        private StreamReader OpenFile()
        {
            return new StreamReader(FilePath);
        }

        private string FilePath
        {
            get { return Components.First().BasePath + Dir + "\\" + File; }
        }

        public void AddComponent(Solution solution)
        {
            Components.Add(solution);
        }

        public string PrintDependencies()
        {
            return string.Join("", dependentProjects.Select(project => Name.Underscored() + "->" + project.Name.Underscored() + "\n").ToArray());
        }

        public string PrintAsNode()
        {
            return string.Format("{0}[fontsize = 12, shape = box]\n", Name.Underscored());
        }

        public void UpdateAssemblyName(IDictionary<string, string> dllNameToProjectName)
        {
            using (StreamReader file = OpenFile())
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.Contains("AssemblyName")) continue;

                    if (assemblyNameRegex.IsMatch(line))
                    {
                        var assemblyName = assemblyNameRegex.Match(line).Value("assemblyname");
                        dllNameToProjectName.Add(assemblyName, Name);
                        AssemblyName = assemblyName;
                    }
                    return;
                }
            }
        }

        public string PrintDependenciesTo(Solution solution)
        {
            var dependenciesToSolution = dependentProjects.Where(project => project.Components.Contains(solution));
            return string.Join("", dependenciesToSolution.Select(project => Name.Underscored() + "->" + project.Name.Underscored() + "\n").ToArray());
        }
    }
}