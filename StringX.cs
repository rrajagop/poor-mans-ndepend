namespace DependencyAnalyser
{
    public static class StringX
    {
        public static string Underscored(this string withDots)
        {
            return withDots.Replace('.', '_');
        }
    }
}
