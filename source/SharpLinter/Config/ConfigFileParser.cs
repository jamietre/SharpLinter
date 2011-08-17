using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JTC.SharpLinter;

namespace JTC.SharpLinter.Config
{
    public class ConfigFileParser
    {
        private static char[] stringSep = new char[] { ',' };
        public string ConfigData { get; set; }
        /// <summary>
        /// Parses key/value data of the format key:value, key2:value2. If value is missing, the string "true" is
        /// returned in its place, making this acceptable for mixed mode parsing.
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> GetKVPSection(string sectionName)
        {
            string[] list = GetSection(sectionName).Split(stringSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in list)
            {
                string[] kvpArray = item.Split(':');
                yield return new KeyValuePair<string, string>(kvpArray[0].Trim(), 
                    kvpArray.Length>1 ? kvpArray[1].Trim() : "true" );
            }
        }
        public IEnumerable<string> GetValueSection(string sectionName, string separators)
        {
            char[] splitSeparators = separators.ToArray();

            string[] list = GetSection(sectionName).Split(splitSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in list)
            {

                string output = item.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
                if (output != String.Empty)
                {
                    yield return output;
                }
            }
        }
        public string GetSection(string sectionName)
        {
            int pos = ConfigData.IndexOf("/*" + sectionName);
            if (pos < 0)
            {
                return String.Empty;
            }

            int endPos = ConfigData.IndexOf(sectionName+"*/", pos);
            if (endPos < 0)
            {
                throw new Exception("Config section '" + sectionName + "' was not closed.");
            }
            return ConfigData.SubstringBetween(pos + sectionName.Length + 2, endPos);
        }


    }
}
