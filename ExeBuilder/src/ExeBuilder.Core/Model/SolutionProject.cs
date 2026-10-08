namespace ExeBuilder.Core.Model
{
    /// <summary>Progetto referenziato da una solution.</summary>
    public sealed class SolutionProject
    {
        public SolutionProject(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
        }

        public string Name { get; private set; }

        public string FullPath { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
