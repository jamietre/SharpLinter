﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
    public static class Utility
    {
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
            string lcaseString = (theString == null ? "" : theString.ToLower());
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
    }
}
