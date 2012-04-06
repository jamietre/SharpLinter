using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;
using System.IO;

namespace JTC.SharpLinter
{
    public enum CompressorType
    {
        yui = 1,
        packer = 2,
        best = 3
    }
    public class SharpCompressor
    {
        public SharpCompressor()
        {
            CompressorType = CompressorType.best;
        }
        public CompressorType CompressorType {get;set;}
        public bool AllowEval
        { get; set; }
        public string Input
        { get; set; }
        protected LinterECMAErrorReporter Reporter = new LinterECMAErrorReporter();

        public bool YUITest(string javascript)
        {

            Clear();
            string compressed;
            return compressYUI(javascript, out compressed);
        }
        public void Clear()
        {
            Reporter.Clear();
            //CompressorType = CompressorType.best;
            Input = null;
            Statistics = null;
            //Input = null;
            Output = null;
            KeepHeader = false;
        }
        protected List<JsLintData> _Errors = new List<JsLintData>();
        public bool Success
        {
            get {
                return Reporter.Errors.Count==0;
            }
        }
        public int ErrorCount
        {
            get
            {
                return _Errors.Count;
            }
        }
        public IEnumerable<JsLintData> Errors
        {
            get
            {
                return Reporter.Errors;
            }
        }
        public string Statistics
        {
            get;
            protected set;
        }
        //public string Input { get; set; }
        public string Output { get; protected set; }
        public bool KeepHeader { get; set; }

        public bool Minimize()
        {
            if (String.IsNullOrEmpty(Input)) {
                throw new Exception("Input must be specified.:");
            }
            if (CompressorType == 0)
            {
                throw new Exception("No compressor type specified.");
            }
            string compressedYui = String.Empty;
            string compressedPacker = String.Empty;
            string header = String.Empty;
            string javascript = Input;
            //string javascript = File.ReadAllText(path);
            if (KeepHeader)
            {
                int pos = javascript.IndexOf("/*");
                int endPos = -1;
                string leadin = String.Empty;
                if (pos >= 0)
                {
                    leadin = javascript.Substring(0, pos);
                    if (leadin.Trim() == string.Empty)
                    {
                        endPos = javascript.IndexOf("*/", pos + 1);
                        header = javascript.Substring(pos, endPos + 2) + Environment.NewLine;
                        javascript = javascript.Substring(endPos + 2);

                    }
                }
            }

            if (CompressorType == CompressorType.yui || CompressorType == CompressorType.best)
            {
                if (!compressYUI(javascript, out compressedYui)) {

                    throw new Exception("The YUI compressor reported errors, which is strange because we already did a dry run to determine the best compressor.");
                }
            }
            if (CompressorType == CompressorType.packer || CompressorType == CompressorType.best)
            {
                JavascriptPacker jsPacker = new JavascriptPacker(JavascriptPacker.PackerEncoding.None, false, false);
                compressedPacker = jsPacker.Pack(javascript);
            }
            CompressorType finalPacker = CompressorType != CompressorType.best ? CompressorType :
                    (compressedYui.Length < compressedPacker.Length ? CompressorType.yui : CompressorType.packer);

            Output = header + (finalPacker == CompressorType.yui ? compressedYui : compressedPacker);

            Statistics = finalPacker.ToString() + ": " + javascript.Length + "/" + Output.Length + ", " 
                + Math.Round(100 * ((decimal)Output.Length / (decimal)javascript.Length), 0) + "%";
            return Success;
        }

        protected bool compressYUI(string input, out string output)
        {
            bool success;
            try
            {
                var reporter = new LinterECMAErrorReporter();
                Yahoo.Yui.Compressor.JavaScriptCompressor compressor;
                compressor = new JavaScriptCompressor(input, true, Encoding.UTF8,
                System.Globalization.CultureInfo.CurrentCulture,
                    AllowEval,
                    reporter);
                output = compressor.Compress(true, true, false, 80);
                success = Reporter.Errors.Count == 0;
            }
            catch
            {
                output = "";
                success = false;
            }

            return success;
        }

    }
}
