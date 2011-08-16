using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JTC.SharpLinter.Config
{

	/// <summary>
	///  Represents configuring the Js Lint(er)
	/// </summary>
	public class JsLintConfiguration
	{
		public JsLintConfiguration()
		{

		}

        /// <summary>
        /// File masks that will be excluded from wildcard matches
        /// </summary>
        public HashSet<string> ExcludeFiles = new HashSet<string>();
		/// <summary>
		///  The Js Lint boolean options specified
		/// </summary>
        protected Dictionary<string, object> Options = new Dictionary<string,object>();
        #region public option methods
        public void SetGlobal(string varName)
        {
             _Predefined.Add(varName);
        }
        public void SetFileExclude(string mask) {
            ExcludeFiles.Add(mask);
        }
        /// <summary>
        /// Returns default value for type if doesn't exist
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        
        public T GetOption<T>(string option)
        {
            option = option.Trim().ToLower();
            object value;
            if (Options.TryGetValue(option, out value))
            {
                if (value is T)
                {
                    return (T)value;
                }
                else
                {
                    throw new Exception("The option '" + option + "' is not of type " + typeof(T).ToString());
                }
            }
            else
            {
                return default(T);
            }
        }
        public bool HasOption(string option)
        {
            return Options.ContainsKey(option.ToLower().Trim());
        }
        public void SetOption(string option)
        {
            SetOption(option, true);
        }
        public void SetOption(string option, object value)
        {
            option = option.Trim().ToLower();

            Tuple<string, Type> optInfo;
            if (Descriptions.TryGetValue(option, out optInfo))
            {
                if (optInfo.Item2 == typeof(bool))
                {
                    bool? val;
                    if (value is bool)
                    {
                        val = (bool)value;
                    }
                    else
                    {
                        val = Utility.StringToBool(value.ToString(), null);
                        if (val == null)
                        {
                            throw new Exception("Unable to interpret boolean value passed with option '" + option + "'");
                        }
                    }
                    Options[option] = (bool)val;
                }
                else if (optInfo.Item2 == typeof(int))
                {

                    int val;
                    if (value is int)
                    {
                        val = (int)value;
                    }
                    else
                    {
                        if (!Int32.TryParse(value.ToString(), out val))
                        {
                            throw new Exception("Unable to interpret integer value passed with option '" + option + "'");
                        }
                    }
                    Options[option] = val;
                }
                else
                {
                    Options[option] = value.ToString();
                }
            } else {
                throw new Exception("Unknown option '" + option + "'");
                
            }
        }

        public int MaxErrors
        {
            get
            {
                int errs = GetOption<int>("maxerr");
                return errs == 0 ? 100 : errs;

            }
        }
        public bool ErrorOnUnused
        {
            get
            {
                return !HasOption("unused") ? true : (bool)Options["unused"];
            }
        }
        public IEnumerable<string> PreDefined
        {
            get
            {
                HashSet<string> list = new HashSet<string>();
                if (HasOption("predef"))
                {
                    string[] predef = GetOption<string>("predef").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in predef)
                    {
                        list.Add(item);
                    }
                }
                foreach (var item in _Predefined)
                {
                    list.Add(item);
                }
                return list;
            }
        }
        protected List<string> _Predefined = new List<string>();
        #endregion

        private static Tuple<string, Type> BoolOpt(string description)
        {
            return Tuple.Create<string,Type>(description,typeof(bool));
        }
		/// <summary>
		///  Descriptions of each option, pulled from Js Lint
		/// </summary>
		protected static readonly Dictionary<string, Tuple<string,Type>> Descriptions = new Dictionary<string, Tuple<string,Type>>() {
					{ "adsafe", BoolOpt("if ADsafe should be enforced") },
					{ "bitwise", BoolOpt("if bitwise operators should not be allowed") },
					{ "browser", BoolOpt("if the standard browser globals should be predefined") },
					{ "cap", BoolOpt("if upper case HTML should be allowed") },
					{ "css", BoolOpt("if CSS workarounds should be tolerated") },
					{ "debug", BoolOpt("if debugger statements should be allowed") },
					{ "devel", BoolOpt("if logging should be allowed (console, alert, etc.)") },
					{ "eqeqeq", BoolOpt("if === should be required") },
					{ "es5", BoolOpt("if ES5 syntax should be allowed") },
					{ "evil", BoolOpt("if eval should be allowed") },
					{ "forin", BoolOpt("if for in statements must filter") },
					{ "fragment", BoolOpt("if HTML fragments should be allowed") },
					{ "immed", BoolOpt("if immediate invocations must be wrapped in parens") },
					{ "laxbreak", BoolOpt("if line breaks should not be checked") },
					{ "newcap", BoolOpt("if constructor names must be capitalized") },
					{ "nomen", BoolOpt("if names should be checked") },
					{ "on", BoolOpt("if HTML event handlers should be allowed") },
					{ "onevar", BoolOpt("if only one var statement per function should be allowed") },
					{ "passfail", BoolOpt("if the scan should stop on first error") },
					{ "plusplus", BoolOpt("if increment/decrement should not be allowed") },
					{ "regexp", BoolOpt("if the . should not be allowed in regexp literals") },
					{ "rhino", BoolOpt("if the Rhino environment globals should be predefined") },
					{ "undef", BoolOpt("if variables should be declared before used") },
					{ "safe", BoolOpt("if use of some browser features should be restricted") },
					{ "windows", BoolOpt("if MS Windows-specigic globals should be predefined") },
					{ "strict", BoolOpt("require the \"use strict\"; pragma") },
					{ "sub", BoolOpt("if all forms of subscript notation are tolerated") },
					{ "white", BoolOpt("if strict whitespace rules apply") },
					{ "widget" , BoolOpt("if the Yahoo Widgets globals should be predefined") },
                    { "maxerr", Tuple.Create<string,Type>("maximum number of errors",typeof(int)) },
                    { "unused" , BoolOpt("show unused local variables") },
                    { "predef" , Tuple.Create<string,Type>("space seperated list of predefined globals", typeof(string)) 

                    }};

		/// <summary>
		///  Returns human readable text description of what the Parse function will process
		/// </summary>
		public static string GetParseOptions()
		{
			StringBuilder returner = new StringBuilder();

			returner.AppendLine("[option : value | option][,option: value | option]...");
			returner.AppendLine();
            //returner.AppendLine("maxerr  : Number - maximum number of errors");
            //returner.AppendLine("predef  : String - space seperated list of predefined globals");
            //returner.AppendLine("unused  : Boolean - If true, errors on unused local vars")m,

			foreach (var optInfo in Descriptions)
			{
				returner.AppendFormat("{0} : ({1}) {1}", optInfo.Key,
                    optInfo.Value.Item2.ToString().AfterLast("."),
                    optInfo.Value.Item1);
				returner.AppendLine();
			}

			return returner.ToString();
		}
        /// <summary>
        /// Merges the options from another config object into this one. The new options supercede if the same are specified.
        /// </summary>
        /// <param name="configuration"></param>
        public void MergeOptions(JsLintConfiguration configuration)
        {
            foreach (var kvp in configuration.Options)
            {
                Options[kvp.Key] = kvp.Value;
            }

            foreach (string file in configuration.ExcludeFiles)
            {
                ExcludeFiles.Add(file);
            }
        }
        public void MergeOptions(string configFile)
        {
            string data= File.ReadAllText(configFile);
            JsLintConfiguration config = ParseConfigFile(data);
            MergeOptions(config);
            //ConfigFileParser parser = new ConfigFileParser();
            
            //foreach (var kvp in parser.GetKVPSection("jslint"))
            //{
            //    SetOption(kvp.Key, kvp.Value);
            //}
            //foreach (var global in parser.GetValueSection("global",","))
            //{
            //    SetGlobal(global);
            //}
            //foreach (var exclude in parser.GetValueSection("exclude","\n,"))
            //{
            //    SetFileExclude(exclude);
            //}
        }
        public IEnumerable<string> GetMatchedFiles(IEnumerable<PathInfo> paths)
        {
            
            foreach (var item in paths)
            {
                string value =item.Path;

                string filter = null;
                if (value.Contains("*"))
                {
                    filter = Path.GetFileName(value);
                    value = value.Substring(0, value.Length - filter.Length);
                }
                if (!Directory.Exists(value))
                {
                    // perhaps its a file?
                    if (!File.Exists(value))
                    {
                        throw new ArgumentException(string.Format("File or directory does not exist: {0}", value));
                    }
                    else
                    {
                        yield return value;
                        continue;
                    }
                }
                List<DirectoryInfo> directorys = new List<DirectoryInfo>() { 
							new DirectoryInfo(value) };
                while (directorys.Count > 0)
                {
                    DirectoryInfo di = directorys[0];
                    directorys.RemoveAt(0);
                    FileInfo[] files;
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        files = di.GetFiles(filter);
                    }
                    else
                    {
                        files = di.GetFiles(filter);
                    }

                    
                    List<string> allFiles = new List<string>();
                    foreach (FileInfo fi in files)
                    {
                        allFiles.Add(fi.FullName);
                    }
                    foreach (string matchedFile in FilePathMatcher.MatchFiles(ExcludeFiles,allFiles, true)) {
                        yield return matchedFile;
                    }
                }

            }
        }

        /// <summary>
        /// Parse a global format config file
        /// </summary>
        /// <param name="configFileData"></param>
        /// <returns></returns>
        public static JsLintConfiguration ParseConfigFile(string configFileData)
        {
            JsLintConfiguration config = new JsLintConfiguration();

            ConfigFileParser parser = new ConfigFileParser();
            parser.ConfigData = configFileData;
            foreach (var kvp in parser.GetKVPSection("jslint"))
            {
                config.SetOption(kvp.Key, kvp.Value);    
            }
            foreach (var item in parser.GetValueSection("global",",")) {
                string[] items = item.Split(':');
                if (items.Length==0 || items.Length > 1 && Utility.IsTrueString(items[1]))
                {
                    config.SetGlobal(item);
                }
            }
            foreach (var item in parser.GetValueSection("exclude","\n,"))
            {
                config.SetFileExclude(item);
            }
            return config;
        }

		/// <summary>
		///  Parses a string and extracts the options, returning a new JsLintConfiguration object
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static JsLintConfiguration ParseString(string s)
		{
			JsLintConfiguration returner = new JsLintConfiguration();

			// if there are no options we return an empty default object
			if (!string.IsNullOrWhiteSpace(s))
			{
				// otherwise, wipe the bool options
				//returner.BoolOptions = (JsLintBoolOption)0;

				// now go through each string
				string[] options = s.Split(',');
				foreach (string option in options)
				{

					string[] optionValue = option.Split(':', '=');

					// test if it is a single value without assigment ("evil" == "evil:true")
					if (optionValue.Length == 1)
					{
						if (optionValue[0].Trim() == "unused")
						{
							returner.SetOption("unused",true);
						}
						else
						{
                            returner.SetOption(optionValue[0], true);
						}
					}
					else if (optionValue.Length == 2)
					{
						// otherwise we have key value pair

						string key = optionValue[0].Trim();
                        returner.SetOption(optionValue[0], optionValue[1].Trim());
					}
					else
					{
						throw new Exception("Unrecognised option format - too many colons");
					}
				}
			}

			return returner;
		}

		/// <summary>
		///  Creates an (javascript compatible) object that JsLint can use for options.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> ToJsOptionVar()
		{
			Dictionary<string, object> returner = new Dictionary<string, object>();

            foreach (var kvp in Options)
            {
                object value = kvp.Value;
                if (kvp.Key == "predef")
                {
                    value = ((string)kvp.Value).Trim().Replace(" ", ",");
                }
                returner[kvp.Key] = value.ToString();
			}			

			return returner;
		}
        public string OptionsToString()
        {
            string result = String.Empty;
            foreach (var kvp in Options)
            {
                if (kvp.Key != "predef")
                {
                    result += (result == String.Empty ? String.Empty : ", ");
                    result += kvp.Key + ": " +
                        (kvp.Value is bool ?
                            ((bool)kvp.Value ? "true" : "false") :
                            kvp.Value.ToString());
                }
            }
            return result;
        }
        public string GlobalsToString()
        {
            
            string result = String.Empty;
            foreach (var item in PreDefined) 
            {
                result +=(result==String.Empty ? String.Empty : ", ") + item;
            }
            return result;
        }
	}
}
