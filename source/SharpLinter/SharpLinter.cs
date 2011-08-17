using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Noesis.Javascript;
using System.IO;
using System.Reflection;
using JTC.SharpLinter.Config;

namespace JTC.SharpLinter
{
	/// <summary>
	///  Constructs an object capable of linting javascript files and returning the result of JS Lint
	/// </summary>
    public class SharpLinter
    {
        private JavascriptContext _context;
        private object _lock = new Object();
      
        /// <summary>
        /// String of the javascript to be validated
        /// </summary>
        public string Javascript
        {
            get;
            set;
        }
        private List<bool> LineExclusion;


        /// <summary>
        /// The script that gets run
        /// </summary>
        public string JSLint
        {
            get;
            protected set;
        }
        public SharpLinter()
        {
            Initialize(string.Empty);
        }
        public SharpLinter(string jsLintSource)
        {
            Initialize(jsLintSource);
        }
        protected void Initialize(string jsLintSource) 
        {

            _context = new JavascriptContext();
			
            if (String.IsNullOrEmpty(jsLintSource))
            {

                using (Stream jslintStream = Assembly.Load("JTC.SharpLinter")
                                                .GetManifestResourceStream(
                                                @"JTC.SharpLinter.fulljslint.js"))
                {
                    using (StreamReader sr = new StreamReader(jslintStream))
                    {
                        JSLint = sr.ReadToEnd();
                    }
                }
            }
            else
            {
                if (!File.Exists(jsLintSource))
                {
                    throw new ArgumentException("'" + jsLintSource + "' is not a valid file path.");
                } else {
                    JSLint = File.ReadAllText(jsLintSource);
                }
            }

            _context.Run(JSLint);

            _context.Run(
                @"lintRunner = function (dataCollector, javascript, options) {
                    JSHINT(javascript,options);
                    
                    var data = JSHINT.data();
                    if  (data) {
                        dataCollector.ProcessData(data);
                    }
                };");
        }
        public LinterType GuessLinterType()
        {
            if (JSLint.IndexOf("var JSHINT = (function () {") > 0)
            {
                return LinterType.JSHint;
            } else {
                return LinterType.JSLint;
            }
        }
		public JsLintResult Lint()
		{
			return Lint(new JsLintConfiguration());
		}
        public JsLintResult Lint(string javascript)
        {
            return Lint(javascript,new JsLintConfiguration());
        }
        public JsLintResult Lint(string javascript, JsLintConfiguration configuration)
        {
            Javascript = javascript;
            return Lint(configuration);
        }
        public JsLintResult Lint(JsLintConfiguration configuration)
        {
			

			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

            bool hasSkips=false;

            lock (_lock)
            {

				LintDataCollector dataCollector = new LintDataCollector(configuration.GetOption<bool>("unused"));

                if (!String.IsNullOrEmpty(configuration.IgnoreStart) && !String.IsNullOrEmpty(configuration.IgnoreEnd))
                {
                    LineExclusion = new List<bool>();
                    using (StringReader reader = new StringReader(Javascript))
                    {
                        string text;
                        int line = 0;
                        int startSkipLine = 0;
                        bool skipping = false;
                        while ((text = reader.ReadLine()) != null)
                        {
                            line++;
                            if (text.IndexOf(skipping ? configuration.IgnoreEnd : configuration.IgnoreStart) >= 0)
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
                        }
                    }
                }
                if (string.IsNullOrEmpty(Javascript))
			    {
                    JsLintData err = new JsLintData();
                    err.Line=0;
                    err.Character=0;
                    err.Reason="The file was empty.";
                    dataCollector.Errors.Add(err);
			    }
                // Setting the externals parameters of the context
				_context.SetParameter("dataCollector", dataCollector);
                _context.SetParameter("javascript", Javascript);
				_context.SetParameter("options", configuration.ToJsOptionVar());


                // Running the script
                _context.Run("lintRunner(dataCollector, javascript, options);");

                JsLintResult result = new JsLintResult();
                if (!hasSkips) {
                    result.Errors = dataCollector.Errors;
                } else {
                    result.Errors = new List<JsLintData>();
                    foreach (var error in dataCollector.Errors) {
                        if (!LineExclusion[error.Line - 1])
                        {
                            result.Errors.Add(error);
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
						ProcessListOfObject(dataDict["unused"], (unused) =>
						{
							JsLintData jsError = new JsLintData();
							if (unused.ContainsKey("line"))
							{
								jsError.Line = (int)unused["line"];
							}

							if (unused.ContainsKey("name"))
							{
								jsError.Reason = string.Format("Unused Variable : {0}", unused["name"]);
							}

							_errors.Add(jsError);
						});
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
