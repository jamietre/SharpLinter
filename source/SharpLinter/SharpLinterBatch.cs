using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JTC.SharpLinter.Config
{
    public class SharpLinterBatch
    {
        public JsLintConfiguration Configuration
        {
            get;
            set;
        }
        public string OutputFormat
        {
            get
            {
                if (!String.IsNullOrEmpty(Configuration.OutputFormat))
                {
                    return Configuration.OutputFormat;
                }
                else
                {
                    return _OutputFormat;
                }
            }
            set
            {
                _OutputFormat = value;
            }
        }
        private string _OutputFormat;
        public IEnumerable<PathInfo> FilePaths { get; set; }
        public SharpLinterBatch(JsLintConfiguration configuration)
        {
            Configuration = configuration;
            OutputFormat = "{0}({1}): ({2}) {3} at character {4}";
        }
        public void Process()
        {
            SharpLinter lint = new SharpLinter(Configuration.JsLintCode);
            List<string> SummaryInfo = new List<string>();
            if (Configuration.Verbose)
            {
                Console.WriteLine("SharpLinter: Beginning processing at {0:MM/dd/yy H:mm:ss zzz}", DateTime.Now);
                Console.WriteLine("Using linter options for " + Configuration.LinterType.ToString(), DateTime.Now);
                Console.WriteLine("LINT options: " + Configuration.OptionsToString());
                Console.WriteLine("LINT globals: " + Configuration.GlobalsToString());
                Console.WriteLine("Sharplint: ignorestart={0}, ignoreend={1}, ignorefile={2}",Configuration.IgnoreStart,Configuration.IgnoreEnd,Configuration.IgnoreFile);
                Console.WriteLine();
            }
            int fileCount = 0;

            List<JsLintData> allErrors = new List<JsLintData>();
            

            foreach (string file in Configuration.GetMatchedFiles(FilePaths))
            {
                bool lintErrors=false;
                bool YUIErrors=false;
                fileCount++;
                string javascript = File.ReadAllText(file);
                if (javascript.IndexOf("/*" + Configuration.IgnoreFile + "*/") >= 0)
                {
                    continue;
                }
                lint.Javascript = javascript;
                JsLintResult result = lint.Lint(Configuration);
                bool hasErrors = result.Errors.Count > 0;

                if (hasErrors)
                {
                    lintErrors = true;
                    foreach (JsLintData error in result.Errors)
                    {
                        error.FilePath = file;
                        allErrors.Add(error);
                    }
                    
                    SummaryInfo.Add(String.Format("{0}: Lint found {1} errors.", file, result.Errors.Count));
                }
                

                SharpCompressor compressor = new SharpCompressor();
                if (Configuration.YUIValidation)
                {
                    compressor.Clear();
                    compressor.AllowEval = Configuration.GetOption<bool>("evil");
                    compressor.KeepHeader = Configuration.MinimizeKeepHeader;
                    compressor.CompressorType = Configuration.CompressorType;

                    hasErrors = !compressor.YUITest(lint.Javascript);
                    
                    if (hasErrors)
                    {
                        YUIErrors = true;
                        foreach (var error in compressor.Errors)
                        {
                            error.FilePath = file;
                            allErrors.Add(error);
                        }
                        
                        SummaryInfo.Add(String.Format("{0}: YUI compressor found {1} errors.", file, compressor.ErrorCount));
                    }
                }

                if (Configuration.MinimizeOnSuccess) {
                    compressor.Clear();
                    compressor.Input = lint.Javascript;
                    compressor.CompressorType = Configuration.CompressorType;
                    compressor.KeepHeader = Configuration.MinimizeKeepHeader;

                    string target = MapFileName(file, Configuration.MinimizeFilenameMask);
                    try
                    {
                        //Delete no matter what - there should never be a mismatch between regular & min
                        if (File.Exists(target))
                        {
                            File.Delete(target);
                        }

                        if (!hasErrors && compressor.Minimize())
                        {
                            File.WriteAllText(target, compressor.Output);
                            SummaryInfo.Add(String.Format("{0}: Compressed to '{1}' ({2})", file, target, compressor.Statistics));
                        }
                    }
                    
                    catch (Exception e)
                    {
                        SummaryInfo.Add(String.Format("{0}: Unable to compress output to '{1}': {2}", file, target, e.Message));
                    }
                }
                allErrors.Sort(LintDataComparer);
                if (!(lintErrors || YUIErrors)) 
                {
                    SummaryInfo.Add(String.Format("{0}: No errors found.", file));
                }
                
            }
            if (Configuration.Verbose)
            {
                // Output file-by-file results at beginning
                foreach (string item in SummaryInfo)
                {
                    Console.WriteLine(item);
                }
            }
            

            if (allErrors.Count > 0)
            {
                if (Configuration.Verbose) {
                Console.WriteLine();
                Console.WriteLine("Error Details:");
                Console.WriteLine();
                    }

                foreach (JsLintData error in allErrors)
                {
                    Console.WriteLine(string.Format(OutputFormat, error.FilePath, error.Line, error.Source, error.Reason, error.Character));
                }
            }
            if (Configuration.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("SharpLinter: Finished processing at {0:MM/dd/yy H:mm:ss zzz}. Processed {1} files.", DateTime.Now, fileCount);
            }
            else
            {
                if (allErrors.Count == 0)
                {
                    Console.WriteLine("SharpLinter: Finished processing {0} file(s) at {1:MM/dd/yy H:mm:ss zzz}, no errors found.", fileCount, DateTime.Now);
                }
            }
        }
        private  string MapFileName(string path, string mask)
        {
            if (mask.OccurrencesOf("*") != 1)
            {
                throw new Exception("Invalid mask '" + mask + "' for compressing output. It must have a single wildcard.");
            }

            string maskStart = mask.Before("*");
            string maskEnd = mask.AfterLast("*").BeforeLast(".");
            string maskExt = mask.AfterLast(".");

            string pathBase = path.BeforeLast(".");
            return maskStart + pathBase + maskEnd + "." + maskExt;

        }

        private int LintDataComparer(JsLintData x, JsLintData y)
        {
            return x.Line.CompareTo(y.Line);
        }
    }
}
