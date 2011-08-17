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
            Yahoo.Yui.Compressor.JavaScriptCompressor compressor;

            try
            {
                compressor = new JavaScriptCompressor(javascript, true, Encoding.UTF8,
                System.Globalization.CultureInfo.CurrentCulture,
                AllowEval, Reporter);
                string compressed = compressor.Compress();
            }
            catch
            {
            }
            return Success;


        }
        public void Clear()
        {
            Reporter.Clear();
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
                if (pos > 0)
                {
                    leadin = javascript.Substring(0, pos);
                }
                if (leadin.Trim() == string.Empty)
                {
                    endPos = javascript.IndexOf("*/", pos + 1);
                    header = javascript.Substring(pos, endPos + 2) + Environment.NewLine;
                    javascript = javascript.Substring(endPos + 2);

                }
            }

            if (CompressorType == CompressorType.yui || CompressorType == CompressorType.best)
            {

                var reporter = new LinterECMAErrorReporter();
                Yahoo.Yui.Compressor.JavaScriptCompressor compressor;
                compressor = new JavaScriptCompressor(javascript, true, Encoding.UTF8,
                System.Globalization.CultureInfo.CurrentCulture,
                    true,
                    reporter);
                compressedYui = compressor.Compress();
            }
            if (CompressorType == CompressorType.packer || CompressorType == CompressorType.best)
            {
                JavascriptPacker jsPacker = new JavascriptPacker(JavascriptPacker.PackerEncoding.None, false, false);

                compressedPacker = jsPacker.Pack(javascript);
            }
            CompressorType finalPacker = CompressorType != CompressorType.best ? CompressorType :
                    (compressedYui.Length < compressedPacker.Length ? CompressorType.yui : CompressorType.packer);

            string compressed = header + (CompressorType == CompressorType.yui ? compressedYui : compressedPacker);

            Statistics = finalPacker.ToString() + ": " + javascript.Length + "/" + compressed.Length + ", " + Math.Round(100 * ((decimal)compressed.Length / (decimal)javascript.Length), 0) + "%";
            return Success;
        }

    }
}
