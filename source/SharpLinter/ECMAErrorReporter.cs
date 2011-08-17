using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;

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
            err.Reason = message;
            Errors.Add(err);
        }
        public void Clear()
        {
            Errors.Clear();
        }
    }
}
