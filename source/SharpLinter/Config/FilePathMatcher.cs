using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JTC.SharpLinter.Config
{
    public static class FilePathMatcher
    {
        /// <summary>
        /// Returns only the files from names that match pattern, unless exclude, in which case only those that don't match
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IEnumerable<string> MatchFiles(string pattern, IEnumerable<string> names, bool exclude)
        {
            string[] patterns = new string[] {pattern};
            return MatchFiles(patterns, names, exclude);

        }
        public static IEnumerable<string> MatchFiles(IEnumerable<string> patterns, IEnumerable<string> names, bool exclude)
        {
            List<string> matches = new List<string>();
            Regex nameRegex=null;
            bool match=false;
            bool noPatterns = true;
            
            foreach (string path in names)
            {
                string cleanPath = path.Replace("/", "\\");
                string fileNameOnly = FileNamePart(path);
                foreach (string pattern in patterns)
                {
                    noPatterns = false;
                    string cleanPattern = pattern.Replace("/", "\\");
                    string namePattern = FileNamePart(cleanPattern);
                    string pathPattern = PathPart(cleanPattern);

                    if (namePattern != String.Empty)
                    {
                        nameRegex = FindFilesPatternToRegex.Convert(namePattern);
                    }

                    
                    match = (pathPattern == String.Empty ? true : MatchPathOnly(cleanPattern, cleanPath)) && (namePattern == String.Empty ? true : nameRegex.IsMatch(fileNameOnly));
                    if (match)
                    {
                        break;
                    }
                }
                if (match != exclude)
                {
                    yield return path;
                }
            }
            // think this was from when we had the patterns/paths loops inverted. Should never be needed (actually causes dups now)
            // .. if the pattern loop is skipped, match will be false, as is correct.

            //if (noPatterns)
            //{
            //    foreach (string name in names)
            //    {
            //        yield return name;
            //    }
            //}
        }
        private static string FileNamePart(string pattern)
        {
            return pattern.Substring(pattern.Length - 1, 1) == "\\" ? String.Empty :
                (pattern.IndexOf("\\") == -1 ? pattern : pattern.AfterLast("\\"));
        }
        private static string PathPart(string pattern)
        {
            return pattern.Substring(pattern.Length - 1, 1) == "\\" ? pattern :
                (pattern.IndexOf("\\") == -1 ? string.Empty : pattern.BeforeLast("\\"));
        }
        
        private static bool MatchPathOnly(string pattern, string path)
        {
            if (pattern.Substring(pattern.Length - 1, 1) != "\\")
            {
                return false;
            }
            if (pattern.IndexOf(":") > 0 || pattern.IndexOf("\\\\") == 0)
            {
                return path.StartsWith(pattern);
            }
            else
            {
                return path.IndexOf(pattern) >= 0;
            }
        }
        
        private static class FindFilesPatternToRegex
        {
            private static Regex HasQuestionMarkRegEx   = new Regex(@"\?", RegexOptions.Compiled);
            private static Regex HasAsteriskRegex       = new Regex(@"\*", RegexOptions.Compiled);
            private static Regex IlegalCharactersRegex  = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
            private static Regex CatchExtentionRegex    = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
            private static string NonDotCharacters      = @"[^.]*";
            public static Regex Convert(string pattern)
            {
                if (pattern == null)
                {
                    throw new ArgumentNullException();
                }
                pattern = pattern.Trim();
                if (pattern.Length == 0)
                {
                    throw new ArgumentException("Pattern is empty.");
                }
                if(IlegalCharactersRegex.IsMatch(pattern))
                {
                    throw new ArgumentException("Patterns contains illegal characters.");
                }
                bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
                bool matchExact = false;
                if (HasQuestionMarkRegEx.IsMatch(pattern))
                {
                    matchExact = true;
                }
                else if(hasExtension)
                {
                    matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;
                }
                string regexString = Regex.Escape(pattern);
                regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
                regexString = Regex.Replace(regexString, @"\\\?", ".");
                if(!matchExact && hasExtension)
                {
                    regexString += NonDotCharacters;
                }
                regexString += "$";
                Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return regex;
            }
        }
    }
}
