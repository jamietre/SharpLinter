using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JTC.SharpLinter
{
    public class LintDataCollector
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
                    JsLintData jsError = null;
                    string unusedList = String.Empty;
                    int unusedCount = 0;
                    ProcessListOfObject(dataDict["unused"], (unused) =>
                    {

                        int line = 0;
                        if (unused.ContainsKey("line"))
                        {
                            line = (int)unused["line"];
                        }
                        if (line != lastLine)
                        {
                            if (jsError != null)
                            {
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
                            unusedList += (unusedCount == 0 ? String.Empty : ", ") + unused["name"];
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
