using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyAnalyser
{
    public class Solutions : Dictionary<string, Solution>
    {
        public Solutions() {}
        public Solutions(IDictionary<string, Solution> collection) : base(collection) {}
        
        public static Solutions Create(string srcRoot, params string[] slnPaths)
        {
            return new Solutions(slnPaths.Select(sln => new Solution(srcRoot, sln)).ToDictionary(sln => sln.NameWithPath, sln => sln));
        }

        public void ProcessDependencies(Projects allProjects) 
        {
            Values.ToList().ForEach(component => allProjects.AddAll(component.AddProjects(allProjects)));
            IDictionary<string, string> dllNameToProjectName = new Dictionary<string, string>();
            allProjects.Values.ToList().ForEach(project => project.UpdateAssemblyName(dllNameToProjectName));
            allProjects.Values.ToList().ForEach(project => project.CalculateDependencies(allProjects, dllNameToProjectName));
        }

        public Solution FindOrCreateSolution(string basePath, string line)
        {
            var solution = Solution.CreateFrom(basePath, line);
            return FindOrCreateSolution(solution);
        }

        private Solution FindOrCreateSolution(Solution solution)
        {
            if (ContainsKey(solution.NameWithPath)) return this[solution.NameWithPath];
            Add(solution.NameWithPath, solution);
            return solution;
        }

        public string GetDependencyGraph(Projects allProjects)
        {
            var answer = "digraph dependencies {\n";
            answer += String.Join("", Values.Select(component => component.SubgraphCluster()).ToArray());
            answer += String.Join("", allProjects.Values.Select(project => project.PrintDependencies()).ToArray());
            answer += "}\n";
            return answer;
        }

    }
}
