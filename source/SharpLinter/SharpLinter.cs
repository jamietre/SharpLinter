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
        #region constructor

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

        #endregion

        #region private methods

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
       
      
        #endregion

        #region public methods

        protected void Configure()
        {
            _isStartScriptRegex = new Regex(@"<script (.|\n)*?type\s*=\s*[""|']text/javascript[""|'](.|\n)*?>");
            // skip anything with a src=".."
            _isStartScriptRegexFail = new Regex(@"<script (.|\n)*?src\s*=\s*[""|'].*?[""|'](.|\n)*?>");
            if (!String.IsNullOrEmpty(Configuration.IgnoreEnd) &&
                !String.IsNullOrEmpty(Configuration.IgnoreStart))
            {
                _isIgnoreStart = new Regex(@"/\*\s*" + Configuration.IgnoreStart + @"\s*\*/");
                _isIgnoreEnd = new Regex(@"/\*\s*" + Configuration.IgnoreEnd + @"\s*\*/");
                isIgnoreStart = isIgnoreStartImpl;
                isIgnoreEnd = isIgnoreEndImpl;
            }
            else
            {
                isIgnoreStart = notImplemented;
                isIgnoreEnd = notImplemented;
            }
            if (!String.IsNullOrEmpty(Configuration.IgnoreFile))
            {
                _isIgnoreFile = new Regex(@"/\\s*" + Configuration.IgnoreFile + @"\s*\*/");
                isIgnoreFile = isIgnoreFileImpl;
            }
            else
            {
                isIgnoreFile = notImplemented;
            }
        }
        public JsLintResult Lint(string javascript)
        {
            StringBuilder finalJs = new StringBuilder();
            Configure();

            lock (_lock)
            {
                bool hasSkips = false;
				LintDataCollector dataCollector = new LintDataCollector(Configuration.GetOption<bool>("unused"));

                LineExclusion = new List<bool>();
                // lines are evaluated, but errors are ignored: we want to use this for blocks excluded 
                // within a javascript file, because otherwise the parser will freak out if other parts of the
                // code wouldn't validate if that block were missing
                bool ignoreErrors=false;
                    
                // lines are not evaluted by the parser at all - in HTML files we want to pretend non-JS lines
                // are not even there.
                bool ignoreLines = Configuration.InputType == InputType.Html;
                    
                int startSkipLine = 0;

                using (StringReader reader = new StringReader(javascript))
                {
                    string text;
                    int line = 0;

                    while ((text = reader.ReadLine()) != null)
                    {
                        line++;

                        if (!ignoreLines 
                            && Configuration.InputType == InputType.Html && isEndScript(text))
                        {
                            ignoreLines = true;
                        }

                        if (!ignoreErrors  && isIgnoreStart(text))
                        {
                            startSkipLine = line;
                            ignoreErrors = true;
                            hasSkips = true;
                        } 
                        // always check for end - if they both appear on a line, don't do anything. should 
                        // always fall back to continuing to check.
                        if (ignoreErrors && isIgnoreEnd(text)) 
                        {
                            ignoreErrors = false;
                        }
                        LineExclusion.Add(ignoreErrors);

                        finalJs.AppendLine(ignoreLines ? "" : text);

                        if (ignoreLines 
                            && Configuration.InputType == InputType.Html 
                            && isStartScript(text))
                        {
                            ignoreLines = false;
                        }
                            
                    }
                }
                if (ignoreErrors)
                {
                    // there was no ignore-end found, so cancel the results 
                    JsLintData err = new JsLintData();
                    err.Line = startSkipLine;
                    err.Character = 0;
                    err.Reason = "An ignore-start marker was found, but there was no ignore-end. Nothing was ignored.";
                    dataCollector.Errors.Add(err);

                    hasSkips = false;
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

                int index = 0;
                while (result.Errors.Count <= Configuration.MaxErrors 
                    && index<dataCollector.Errors.Count)
                {
                    var error = dataCollector.Errors[index++];
                    if (!hasSkips)
                    {
                        result.Errors.Add(error);
                    }
                    else
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
                    }
                }
                // if we went over, mark that there were more errors and remove last one
                if (result.Errors.Count > Configuration.MaxErrors)
                {
                    result.Errors.RemoveAt(result.Errors.Count - 1);
                    result.Limited = true;
                }
                
                return result;
            }


        }

        #endregion

        #region private methods


        Regex _isStartScriptRegex= new Regex(@"<script (.|\n)*?type\s*=\s*[""|']text/javascript[""|'](.|\n)*?>");
        // skip anything with a src=".."
        Regex _isStartScriptRegexFail = new Regex(@"<script (.|\n)*?src\s*=\s*[""|'].*?[""|'](.|\n)*?>");


        /// <summary>
        /// Returns true if a script appears on this line. 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected bool isStartScript(string text)
        {

            bool result = false;
            var matches = _isStartScriptRegex.Matches(text);
            if (matches.Count > 0)
            {
                // just check the last one, if for some reason there's a block
                // opened & closed and followed by an include all on one line, then
                // just don't deal with it.
                result = !_isStartScriptRegexFail.IsMatch(matches[matches.Count - 1].Value);
            }
            return result;
        }
        Regex _isEndScriptRegex = new Regex(@"</script.*?>");
        protected bool isEndScript(string text)
        {
            return _isEndScriptRegex.IsMatch(text);
        }

        Regex _isIgnoreStart;
        Regex _isIgnoreEnd;
        Regex _isIgnoreFile;

        protected bool notImplemented(string what)
        {
            return false;
        }
        protected Func<string, bool> isIgnoreStart;
        protected bool isIgnoreStartImpl(string text)
        {
            return _isIgnoreStart.IsMatch(text);
        }
        protected Func<string, bool> isIgnoreEnd;
        protected bool isIgnoreEndImpl(string text)
        {
            return _isIgnoreEnd.IsMatch(text);
        }
        protected Func<string,bool> isIgnoreFile;
        protected bool isIgnoreFileImpl(string text)
        {
            return _isIgnoreFile.IsMatch(text);
        }
        #endregion
    }
}
