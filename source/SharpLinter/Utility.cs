using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JTC.SharpLinter
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the text between startIndex and endIndex (exclusive of endIndex)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public static string SubstringBetween(this string text, int startIndex, int endIndex)
        {
            return (text.Substring(startIndex, endIndex - startIndex));
        }
        public static string AfterLast(this string text, string find)
        {
            int index = text.LastIndexOf(find);
            if (index < 0 || index + find.Length >= text.Length)
            {
                return (String.Empty);
            }
            else
            {
                return (text.Substring(index + find.Length));
            }
        }
        /// <summary>
        /// Removes /r /n /t and  spaces
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveWhitespace(this string text)
        {
            int pos=-1;
            int len=text.Length;
            HashSet<char> removeList = new HashSet<char>("\r\n\t ");
            StringBuilder output = new StringBuilder();
            while (++pos < len)
            {
                if (!removeList.Contains(text[pos]))
                {
                    output.Append(text[pos]);
                }
                
            }
            return output.ToString();
        }
    }
    public static class Utility
    {
        /// <summary>
        /// Qualifies a relative path file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ResolveRelativePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return Directory.GetCurrentDirectory() + "\\" + path;
            }
            else
            {
                return path;
            }
        }
        /// <summary>
        /// Evaluates the string to determine whether the value is true or false. Valid true strings are any form of "yes," "true," "on," or the digit "1"
        /// False is always returned otherwise.
        /// </summary>
        /// <param name="theString"></param>
        /// <returns></returns>
        public static bool IsTrueString(string theString)
        {
            return ((bool)StringToBool(theString, false));
        }

        /// <summary>
        /// Like IsTrueString, but if a true or false value is not matched, the default value is returned. The default can be null if no known
        /// true/false strings are matched.
        /// </summary>
        /// <param name="theString"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool? StringToBool(string theString, bool? defaultValue)
        {
            string lcaseString = (theString == null ? "" : theString.ToLower().Trim());
            if (lcaseString == "true" || lcaseString == "yes" || lcaseString == "y" || lcaseString == "1" || lcaseString == "on")
            {
                return (true);
            }
            else if (lcaseString == "false" || lcaseString == "no" || lcaseString == "n" || lcaseString == "0" || lcaseString == "off")
            {
                return (false);
            }
            else
            {
                return (defaultValue);
            }

        }
        public static string BeforeIncluding(this string text, string find)
        {
            int index = text.IndexOf(find);
            if (index < 0 || index == text.Length)
            {
                return (String.Empty);
            }
            else
            {
                return (text.Substring(0, index+find.Length));
            }
        }
        public static string Before(this string text, string find)
        {
            int index = text.IndexOf(find);
            if (index < 0 || index == text.Length)
            {
                return (String.Empty);
            }
            else
            {
                return (text.Substring(0, index));
            }
        }
        public static string BeforeLast(this string text, string find)
        {
            int index = text.LastIndexOf(find);
            if (index >= 0)
            {
                return (text.Substring(0, index));
            }
            else
            {
                return String.Empty;
            }
        }

        public static int OccurrencesOf(this string text, string find)
        {
            bool finished = false;
            int pos=0;
            int occurrences=0;
            while (!finished)
            {
                pos = text.IndexOf(find,pos);
                if (pos >= 0)
                {
                    occurrences++;
                    pos++;
                    if (pos == text.Length)
                    {
                        finished = true;
                    }
                }
                else
                {
                    finished = true;
                }

            }
            return occurrences;

        }
        public static string AddListItem(this string list, string value, string separator)
        {
            if (String.IsNullOrEmpty(value))
            {
                return list.Trim();
            }
            if (list == null)
            {
                list = String.Empty;
            }
            else
            {
                list = list.Trim();
            }

            int pos = (list + separator).IndexOf(value + separator);
            if (pos < 0)
            {
                if (list.LastIndexOf(separator) == list.Length - separator.Length)
                {
                    // do not add separator - it already exists
                    return list + value;
                }
                else
                {
                    return (list + (list == "" ? "" : separator) + value);
                }
            }
            else
            {
                // already has value
                return (list);
            }
        }
    }

}
