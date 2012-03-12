using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Noesis.Javascript;
using JTC.SharpLinter.Config;


namespace JTC.SharpLinter
{
	/// <summary>
	///  Constructs an object capable of linting javascript files and returning the result of JS Lint
	/// </summary>
    public class SharpLinter
    {
        private IJSEngineWrapper _context;
        private object _lock = new Object();
      
        /// <summary>
        /// Map of lines that should be excluded (true for an index means exclude that line)
        /// </summary>
        protected List<bool> LineExclusion;

        /// <summary>
        /// The script that gets run
        /// </summary>
        protected string JSLint;

        protected JsLintConfiguration Configuration;
        public SharpLinter(JsLintConfiguration config)
        {
            Configuration = config;
            Initialize();
        }
        
        protected void Initialize() 
        {
            _context = new Engines.Neosis();
            //_context = new JavascriptContext();
			
            if (String.IsNullOrEmpty(Configuration.JsLintCode))
            {
                throw new Exception("No JSLINT/JSHINT code was specified in the configuration.");
            }
            else
            {
                JSLint = Configuration.JsLintCode;
            }

            _context.Run(JSLint);

            string func = Configuration.LinterType == LinterType.JSHint ? "JSHINT" : "JSLINT";
            string run =
                @"lintRunner = function (dataCollector, javascript, options) {
                    JSLINT(javascript,options);
                    
                    var data = JSLINT.data();
                    if  (data) {
                        dataCollector.ProcessData(data);
                    }
                };".Replace("JSLINT", func);
            _context.Run(run);
        }
        /// <summary>
        /// Returns true if a script appears on this line. 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected bool isStartScript(string text)
        {
            string expr =  @"<script (.|\n)*?type\s*=\s*[""|']text/javascript[""|'](.|\n)*?>";
            string ignore = @"<script (.|\n)*?src\s*=\s*[""|'].*?[""|'](.|\n)*?>";

            var mustMatch= new Regex(expr);
            var mustNotMatch = new Regex(ignore);
            bool result = false;
            var matches = mustMatch.Matches(text);
            if (matches.Count>0)
            {
                // just check the last one, if for some reason there's a block
                // opened & closed and followed by an include all on one line, then
                // just don't deal with it.
                result = !mustNotMatch.IsMatch(matches[matches.Count-1].Value);
            }
            return result;
        }
        protected bool isEndScript(string text)
        {
            string expr = @"</script.*?>";
            var regex = new Regex(expr);
            return regex.IsMatch(text);
        }
        public JsLintResult Lint(string javascript)
        {
            StringBuilder finalJs = new StringBuilder();

            lock (_lock)
            {
                bool hasSkips = false;
				LintDataCollector dataCollector = new LintDataCollector(Configuration.GetOption<bool>("unused"));

                if (!String.IsNullOrEmpty(Configuration.IgnoreStart) && !String.IsNullOrEmpty(Configuration.IgnoreEnd))
                {
                    LineExclusion = new List<bool>();
                    // lines are evaluated, but errors are ignored
                    bool skipping=false;
                    
                    // lines are not evaluted by the parser
                    bool ignoring = Configuration.InputType == InputType.Html;
                    
                    int startSkipLine = 0;

                    using (StringReader reader = new StringReader(javascript))
                    {
                        string text;
                        int line = 0;

                        while ((text = reader.ReadLine()) != null)
                        {
                            line++;

                            if (!ignoring && Configuration.InputType == InputType.Html && isEndScript(text))
                            {
                                ignoring = true;
                            }

                            if (text.IndexOf("/*" + (skipping ? Configuration.IgnoreEnd : Configuration.IgnoreStart) + "*/") >= 0)
                            {
                                if (!skipping)
                                {
                                    startSkipLine = line;
                                    skipping = true;
                                    hasSkips = true;
                                }
                                else
                                {
                                    skipping = false;
                                }
                            }
                            LineExclusion.Add(skipping);

                            finalJs.AppendLine(ignoring ? "" : text);

                            if (ignoring && Configuration.InputType == InputType.Html && isStartScript(text))
                            {
                                ignoring = false;
                            }
                            
                        }
                    }
                    if (skipping)
                    {
                        // there was no ignore-end found, so cancel the results 
                        JsLintData err = new JsLintData();
                        err.Line = startSkipLine;
                        err.Character = 0;
                        err.Reason = "An ignore start marker was found, but there was no ignore-end. Nothing was ignored.";
                        dataCollector.Errors.Add(err);

                        hasSkips = false;
                    }
                }

                if (finalJs.Length == 0)
                {
                    JsLintData err = new JsLintData();
                    err.Line = 0;
                    err.Character = 0;
                    err.Reason = "The file was empty.";
                    dataCollector.Errors.Add(err);
                }
                else
                {
                    // Setting the externals parameters of the context
                    _context.SetParameter("dataCollector", dataCollector);
                    _context.SetParameter("javascript", finalJs.ToString());
                    _context.SetParameter("options", Configuration.ToJsOptionVar());


                    // Running the script
                    _context.Run("lintRunner(dataCollector, javascript, options);");
                }

                JsLintResult result = new JsLintResult();
                result.Errors = new List<JsLintData>();
                if (!hasSkips)
                {
                    for (int i = 0; i < Math.Min(Configuration.MaxErrors, dataCollector.Errors.Count); i++)
                    {
                        result.Errors.Add(dataCollector.Errors[i]);
                    }
                }
                else
                {

                    foreach (var error in dataCollector.Errors)
                    {

                        if (error.Line >= 0 && error.Line < LineExclusion.Count)
                        {
                            if (!LineExclusion[error.Line - 1])
                            {
                                result.Errors.Add(error);
                            }
                        }
                        else
                        {
                            result.Errors.Add(error);
                        }
                        if (result.Errors.Count == Configuration.MaxErrors)
                        {
                            break;
                        }
                    }
                }
                return result;
            }
            
            
        }

		private class LintDataCollector
		{
			private List<JsLintData> _errors = new List<JsLintData>();
			private bool _processUnuseds = false;

			public List<JsLintData> Errors
			{
				get { return _errors; }
			}

			public LintDataCollector(bool processUnuseds)
			{
				_processUnuseds = processUnuseds;
			}
            public object predef { get; set; }

			public void ProcessData(object data)
			{
				Dictionary<string, object> dataDict = data as Dictionary<string, object>;

				if (dataDict != null)
				{

					if (dataDict.ContainsKey("errors"))
					{
						ProcessListOfObject(dataDict["errors"], (error) =>
						{
							JsLintData jsError = new JsLintData();
                            jsError.Source = "lint";
							if (error.ContainsKey("line"))
							{
								jsError.Line = (int)error["line"];
							}

							if (error.ContainsKey("character"))
							{
								jsError.Character = (int)error["character"];
							}

							if (error.ContainsKey("reason"))
							{
								jsError.Reason = (string)error["reason"];
							}

							_errors.Add(jsError);
						});
					}

					if (_processUnuseds && dataDict.ContainsKey("unused"))
					{
                        int lastLine = -1;
                        JsLintData jsError=null;
                        string unusedList = String.Empty;
                        int unusedCount = 0;
						ProcessListOfObject(dataDict["unused"], (unused) =>
						{

                            int line = 0;
							if (unused.ContainsKey("line"))
							{
								line = (int)unused["line"];
							}
                            if (line!=lastLine) {
                                if (jsError != null) {
                                    jsError.Reason = "Unused Variable" + (unusedCount > 1 ? "s " : " ") + unusedList;
                                    _errors.Add(jsError);
                                    
                                }
                                jsError = new JsLintData();
                                jsError.Source = "lint";
                                jsError.Character = -1;
                                jsError.Line = line;
                                unusedCount = 0;
                                unusedList = String.Empty;
                            }
                            
							if (unused.ContainsKey("name"))
							{
                               unusedList += (unusedCount==0 ? String.Empty : ", ") + unused["name"];
                               unusedCount++;
							}
                            lastLine = line;
						});
                        jsError.Reason = "Unused Variable" + (unusedCount > 1 ? "s " : " ") + unusedList;
                        _errors.Add(jsError);
					}
				}
			}

			private void ProcessListOfObject(object obj, Action<Dictionary<string, object>> processor)
			{
				object[] array = obj as object[];

				if (array != null)
				{
					foreach (object objItem in array)
					{
						Dictionary<string, object> objItemDictionary = objItem as Dictionary<string, object>;

						if (objItemDictionary != null)
						{
							processor(objItemDictionary);
						}
					}
				}
			}
		}
    }
}
