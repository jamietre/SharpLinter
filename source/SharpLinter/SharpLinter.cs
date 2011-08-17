using System;
using System.Collections.Generic;
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
        ///  The path to the target of the linting.
        /// </summary>
        public string TargetPath
        { 
            get {
                return _TargetPath;
            } 
            set
            {
                _TargetPath = value;
                Javascript = File.ReadAllText(value);
            } 
        }
        private string _TargetPath = null;
      
        /// <summary>
        /// String of the javascript to be validated
        /// </summary>
        public string Javascript
        {
            get;
            set;
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
			string jslint = "";
            if (String.IsNullOrEmpty(jsLintSource))
            {

                using (Stream jslintStream = Assembly.Load("JTC.SharpLinter")
                                                .GetManifestResourceStream(
                                                @"JTC.SharpLinter.fulljslint.js"))
                {
                    using (StreamReader sr = new StreamReader(jslintStream))
                    {
                        jslint = sr.ReadToEnd();
                    }
                }
            }
            else
            {
                if (!File.Exists(jsLintSource))
                {
                    throw new ArgumentException("'" + jsLintSource + "' is not a valid file path.");
                } else {
                    jslint = File.ReadAllText(jsLintSource);
                }
            }
            
            _context.Run(jslint);

            _context.Run(
                @"lintRunner = function (dataCollector, javascript, options) {
                    JSHINT(javascript,options);
                    var data = JSHINT.data();
                    if  (data) {
                        dataCollector.ProcessData(data);
                    }
                };");
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
			if (string.IsNullOrEmpty(Javascript))
			{
				throw new ArgumentNullException("javascript");
			}

			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

            lock (_lock)
            {
				LintDataCollector dataCollector = new LintDataCollector(configuration.GetOption<bool>("unused"));
                // Setting the externals parameters of the context
				_context.SetParameter("dataCollector", dataCollector);
                _context.SetParameter("javascript", Javascript);
				_context.SetParameter("options", configuration.ToJsOptionVar());

                // Running the script
                _context.Run("lintRunner(dataCollector, javascript, options);");

                JsLintResult result = new JsLintResult() { Errors = dataCollector.Errors };

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
