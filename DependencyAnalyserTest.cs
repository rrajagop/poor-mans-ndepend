using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using NUnit.Framework;


// Overall structure of the domain model
// Solution -> List<Project> (list of all the projects included in this solution)
// Project -> List<Project> DependentProjects (list of projects which this project needs to build succesfully - this includes dll references)
// Project -> List<Project> DependingProjects (list of projects which need this project to build successfully - this includes dll references)
// Project -> List<Solution> Components (list of solutions which this project is a part of)
// TODO: NOTE: There is some code to filter out projects which have Test in their names - might want to get rid of this?.

// Collections:
// Solutions - Dictionary of Solutions keyed by name of sln file
// allProjects - Dictionary of Projects keyed by name of csproj 

// Use LINQ to traverse this structure and answer the queries you want answered - see examples below.
// You can also print out a couple of different digraphs which can be pasted into gvedit (from graphviz: http://www.graphviz.org/Download.php)
// and try to generate pretty pictures - be warned, the pictures are almost never pretty.

namespace DependencyAnalyser
{


    [TestFixture]
    public class DependencyAnalyserTest
    {
        [Test]
        public void Main()
        {
            const string srcRoot = @"C:\path\to\root\of\source\code\";

            var components = Solutions.Create(srcRoot,
                @"solution1\solution1.sln",
                @"solution2\solution2.sln",
                @"solution3\solution3.sln",
                @"solution4\solution4.sln",
                @"solution5\solution5.sln",
                @"solution6\solution6.sln",
); 
            
            var allProjects = new Projects();
            components.ProcessDependencies(allProjects);
            
            WriteToFile("projectDependenciesGroupedBySolution.txt", components.GetDependencyGraph(allProjects));
            
            var answer = "digraph solution_dependencies  {\n";
            answer += string.Join("", components.Values.Distinct().Select(s => s.ToNode() + "\n").ToArray());
            foreach (var component in components)
            {
                answer += string.Join("", component.Value.Projects.SelectMany(p => p.DependentProjects.SelectMany(dp => dp.Components)).Distinct()
                    .Where(ds => ds.Name != component.Value.Name)
                    .Select(s => component.Value.Name.Underscored() + "->" + s.Name.Underscored() + "\n").ToArray());
            }
            answer += "}\n";
            WriteToFile("interSolutionDependencies.txt", answer);
            

            var currentProjects = ProjectsNamed(allProjects, new List<string>()
            {
                "AssemblyName.Of.Project1",
                "AssemblyName.Of.Project2",
                "AssemblyName.Of.Project3",
                "AssemblyName.Of.Project4",
            }).ToList();

            var currentSolutions = currentProjects.SelectMany(p => p.Components).Distinct().ToList();

            var dependentProjects = DependentProjects(currentProjects);
            var dependentSolutions = DependentSolutions(currentProjects);

            var dependentSolutionOfCurrentSolution = DependentSolutions(currentSolutions.SelectMany(solution => solution.Projects));

            var dependingSolutionsOnCurrentSolution =
                currentSolutions.SelectMany(s => s.Projects).SelectMany(p => p.DependingProjects).SelectMany(p => p.Components).Distinct().ToList();
        }

        public IEnumerable<Project> ProjectsNamed(Projects allProjects, List<string> names)
        {
            var lowerNames = names.Select(s => s.ToLowerInvariant());
            return allProjects.Values.Where(p => lowerNames.Any(s => p.AssemblyName.ToLowerInvariant().Contains(s)));
        }

        public IEnumerable<Solution> DependentSolutions(IEnumerable<Project> projects)
        {
            return projects.SelectMany(p => p.DependentProjects.SelectMany(dp => dp.Components)).Distinct();
        }

        public IEnumerable<Project> DependentProjects(IEnumerable<Project> projects)
        {
            return projects.SelectMany(p => p.DependentProjects).Distinct();
        }

        public string PrintableNames(IEnumerable<IDependency> dependencies)
        {
            return string.Join(", ", dependencies.Select(d => d.Name).ToArray());
        }

        private void WriteToFile(string fileName, string content)
        {
            using (var textWriter = new StreamWriter(File.Create(fileName)))
            {
                textWriter.Write(content);
            }
        }
    }
}
