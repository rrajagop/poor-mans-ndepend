using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace DependencyAnalyser
{
    public class Solution : IDependency
    {
        public string Name { get; set; }
        public string BasePath { get; private set; }
        public List<Project> Projects { get; private set; }
        public IDictionary<string, Object> Extensions { get; set; }

        private string Path { get; set; }

        public static Solution CreateFrom(string srcPath, string lineInFile)
        {
            var match = Regex.Match(lineInFile, ".*\\.\\.\\\\(?<solution>.*\\.sln)\\*\\*\\*$");
            if (!match.Success) return null;
            return new Solution(srcPath, match.Value("solution").Replace("/", "\\"));
        }

        public string NameWithPath
        {
            get { return BasePath + "\\" + Name; }
        }

        public override string ToString()
        {
            return string.Format("{1}\\{0}", Name, Path);
        }

        private static readonly Regex slnNameMatcher = new Regex("(?<basePath>.*\\\\)(?<slnName>[^\\\\]*)\\.sln");

        private static readonly Regex projectRegex =
            new Regex(
                "Project\\(\\\"\\{(?<GUID1>[0-9A-F]{8}\\-[0-9A-F]{4}\\-[0-9A-F]{4}\\-[0-9A-F]{4}\\-[0-9A-F]{12})\\}\\\"\\) = \\\"(?<name>[^\"]*)\\\", \\\"(?<dir>[^\"\\\\]*)\\\\(?<file>[^\"]*\\.csproj)\\\", \\\"\\{(?<GUID2>[0-9A-F]{8}\\-[0-9A-F]{4}\\-[0-9A-F]{4}\\-[0-9A-F]{4}\\-[0-9A-F]{12})\\}\\\"");

        public Solution(string srcRoot, string slnPath)
        {
            if (!slnNameMatcher.IsMatch(slnPath))
                throw new ApplicationException("Couldn't find the name of the solution for: " + slnPath);
            var match = slnNameMatcher.Match(slnPath);
            Name = match.Value("slnName");
            Path = srcRoot + slnPath;
            BasePath = srcRoot + match.Value("basePath");
            Projects = new List<Project>();
        }

        public List<Project> AddProjects(Projects allProjects)
        {
            var answer = new List<Project>();
            using (var reader = new StreamReader(Path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!projectRegex.IsMatch(line)) continue;
                    var match = projectRegex.Match(line);
                    if (match.Value("name").Contains("Test")) continue;
                    AddProject(allProjects.FindOrCreateProject(match.Value("file"), match.Value("dir"), match.Value("name")));
                }
            }
            return answer;
        }

        private void AddProject(Project project)
        {
            Projects.Add(project);
            project.AddComponent(this);
        }

        public string SubgraphCluster()
        {
            var answer = string.Format("subgraph cluster_{0} ", Name.Underscored()) + "{\n";
            answer += string.Format("label = {0};\n", Name.Underscored());
            answer += string.Join("", Projects.Select(project => project.PrintAsNode()).ToArray());
            answer += "}\n";
            return answer;
        }


        public string GetDependencyGraph()
        {
            var relatedComponents = FindRelatedComponents().Distinct().ToList();
            relatedComponents.Remove(this);

            var allRelatedProjects = relatedComponents.SelectMany(solution => solution.Projects);

            var answer = "digraph " + Name.Underscored() + " {\n";
            
            answer += SubgraphCluster();
            answer += String.Join("", relatedComponents.Select(component => component.SubgraphCluster()).ToArray());

            answer += String.Join("", Projects.Select(project => project.PrintDependencies()).ToArray());
            answer += String.Join("", allRelatedProjects.Select(project => project.PrintDependenciesTo(this)).ToArray());
            
            answer += "}\n";
            return answer;
        }

        public string ToNode()
        {
            return string.Format("{0}[fontsize = 12, shape = box]", Name.Underscored());
        }

        private List<Solution> FindRelatedComponents()
        {
            //if (level != 1) throw new NotImplementedException("Need to implement this for levels other than 1");
            return Projects.SelectMany(project => project.DependentProjects.SelectMany(dep => dep.Components)).ToList();
        }      
    }
}