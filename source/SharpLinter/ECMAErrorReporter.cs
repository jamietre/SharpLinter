using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;
using System.IO;

namespace JTC.SharpLinter
{
    public class LinterECMAErrorReporter : EcmaScript.NET.ErrorReporter
    {
        public List<JsLintData> Errors = new List<JsLintData>();

        public void Error(string message, string sourceName, int line, string lineSource, int lineOffset)
        {
            AddErrorFor(line, lineOffset, "Error: " + message);
        }

        public EcmaScript.NET.EcmaScriptRuntimeException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset)
        {
            AddErrorFor(line, lineOffset, "Runtime error: " + message);
            return new EcmaScript.NET.EcmaScriptRuntimeException(message);
        }

        public void Warning(string message, string sourceName, int line, string lineSource, int lineOffset)
        {
            AddErrorFor(line, lineOffset, "Warning: " + message);
        }
        protected void AddErrorFor(int line, int offset, string message)
        {
            JsLintData err = new JsLintData();
            err.Source = "yui";
            err.Line = Math.Max(0,line);
            err.Character = offset;
            using (StringReader reader = new StringReader(message))
            {
                string text;
                int lineNum = 0;
                while ((text = reader.ReadLine()) != null)
                {
                    if (lineNum != 0)
                    {
                        text = Environment.NewLine + "    " + text;
                    }
                    err.Reason += text;
                    lineNum++;
                }
            }
            Errors.Add(err);
        }
        public void Clear()
        {
            Errors.Clear();
        }
    }
}
