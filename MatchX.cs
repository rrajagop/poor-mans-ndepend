using System;
using System.Text.RegularExpressions;

namespace DependencyAnalyser
{
    public static class MatchX
    {
        public static string Value(this Match match, string key)
        {
            var value = match.Groups[key].ToString();
            if (value == string.Empty) 
                Console.Error.WriteLine("Sanity: " + match + " doesn't have " + key);
            return value;
        }
    }
}