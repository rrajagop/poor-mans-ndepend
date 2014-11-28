using System.Collections.Generic;

namespace DependencyAnalyser
{
    public class Projects : Dictionary<string, Project>
    {
        public void Add(Project item)
        {
            this[item.Name] = item;
        }

        public void AddAll(List<Project> items)
        {
            items.ForEach(Add);
        }

        public void AddIgnoredDependency(string dependencyName)
        {
            ignoredDependencies.Add(dependencyName);
        }        

        public HashSet<string> IgnoredDependencies
        {
            get { return ignoredDependencies; }
        }
        private readonly HashSet<string> ignoredDependencies = new HashSet<string>();

        public Project FindOrCreateProject(string file, string dir, string name)
        {
            if (!ContainsKey(name)) this[name] = new Project(file, dir, name);
            return this[name];
        }
    }
}