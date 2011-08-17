using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JTC.SharpLinter;
using JTC.SharpLinter.Config;

/*
 * 
 * SharpLinter
 * (c) 2011 James Treworgy
 * 
 * Based on code originally created by Luke Page/ScottLogic: http://www.scottlogic.co.uk/2010/09/js-lint-in-visual-studio-part-1/
 * 
 */
namespace ConsoleApplication1
{

    class Program
    {
        static void Main(string[] args)
        {
			if (args.Length == 0)
			{
				Console.WriteLine("SharpLinter [-f file.js] [-[r]d] /directory [-c sharplinter.conf] [-o options]");
                Console.WriteLine("            [-j jslint.js] [-jr jslint | jshint] [-y] [-p yui | packer | best] [-ph] [-k] ");
                Console.WriteLine("            [-is text] [-ie text] [-if text]");
				Console.WriteLine();
				Console.Write("Options Format:");
				Console.WriteLine(JsLintConfiguration.GetParseOptions());
				Console.WriteLine();
				Console.WriteLine("E.g.");
				Console.WriteLine("JsLint -f input.js -f input2.js");
				Console.WriteLine("JsLint -f input.js -o \"evil=False,eqeqeq,predef=Microsoft System\"");
				return;
			}

            string commandlineConfig = String.Empty;
            string globalConfigFile = String.Empty;
            string excludeFiles = String.Empty;

            string jsLintSource = String.Empty;
            HashSet<PathInfo> FilePaths = new HashSet<PathInfo>();

            bool YUIValidation = false;
            bool readKey = false;
            bool minimizeOutput = false;
            bool packKeepHeader = false;
            string minimizeMask = String.Empty;
            string ignoreStart = null;
            string ignoreEnd = null;
            string ignoreFile = null;
            LinterType linterType = 0;

            CompressorType compressorType = 0;
            
			for (int i = 0; i < args.Length; i++ )
			{
				string arg = args[i].Trim().ToLower();
                string value = args.Length > i+1 ? args[i + 1] : String.Empty;
                string value2 = args.Length > i+2 ? args[i + 2] : String.Empty;
				//string filter = null;
				switch (arg)
				{
                    case "-if":
                        ignoreFile = value;
                        break;
                    case "-is":
                        ignoreStart = value;
                        break;
                    case "-ie":
                        ignoreEnd = value;
                        break;
                    case "-jr":
                        linterType = value=="jslint" ? LinterType.JSLint :
                            (value=="jshint" ? LinterType.JSHint : 0);
                        if (linterType == 0)
                        {
                            Console.WriteLine(String.Format("Unknown lint rule type {0}", value));
                            return;
                        }
                        break;
                    case "-p":

                        if (!Enum.TryParse<CompressorType>(value, out compressorType))
                        {
                            Console.WriteLine(String.Format("Unknown pack option {0}", value));
                            return;
                        }
                        minimizeOutput = true;
                        minimizeMask = value2;
                        i+=2;
                        break;
                    case "-ph":
                        packKeepHeader = true;
                        break;
                    case "-y":
                        YUIValidation = true;
                        break;
                    case "-c":
                        if (!String.IsNullOrEmpty(globalConfigFile))
                        {
                            Console.WriteLine("Multiple config files specified.");
                        }
                        globalConfigFile = value;
                        i++;
						break;
					case "-f":
						FilePaths.Add(new PathInfo(value,false));
                        i++;
						break;
                    case "-j":
                        if (File.Exists(value)) {
                            jsLintSource = value;
                        } else {
                            Console.WriteLine(String.Format("Cannot find JSLint source file {0}",value));
                            return;
                        }
                        i++;
                        break;
                    case "-k":
                        readKey = true;
                        break;
					case "-d":
					case "-rd":
                        FilePaths.Add(new PathInfo(value, arg == "-rd"));
                        i++;
						break;
					case "-o":
                        commandlineConfig = commandlineConfig.AddListItem(value, " ");
                        i++;
						break;
                    case "-x":
                       excludeFiles = excludeFiles.AddListItem(value," ");
                        i++;
                        break;
				}
			}
            Console.WriteLine("Beginning processing at {0:MM/dd/yy H:mm:ss zzz}", DateTime.Now);
            SharpLinter lint = new SharpLinter(jsLintSource);
            JsLintConfiguration configuration = new JsLintConfiguration();

            configuration.IgnoreStart = ignoreStart;
            configuration.IgnoreEnd = ignoreEnd;
            configuration.IgnoreFile = ignoreFile;

            if (linterType == 0)
            {
                linterType = lint.GuessLinterType();
            }
            configuration.LinterType = linterType;
            Console.WriteLine("Using linter options for "+linterType.ToString(), DateTime.Now);
            
            if (!String.IsNullOrEmpty(globalConfigFile))
            {
                if (File.Exists(globalConfigFile))
                {
                    try
                    {
                        configuration.MergeOptions( JsLintConfiguration.ParseConfigFile(File.ReadAllText(globalConfigFile),linterType));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        goto exit;
                    }
                }
                else
                {
                    Console.WriteLine(String.Format("Cannot find global configuration file {0}", globalConfigFile));
                    goto exit;
                }
            }
            if (commandlineConfig != null)
            {
                try
                {
                    configuration.MergeOptions(JsLintConfiguration.ParseString(commandlineConfig,linterType));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    goto exit;
                }

            }


            // collect details to output at the end
            List<string> SummaryInfo = new List<string>();

            
            Console.WriteLine("LINT options: " + configuration.OptionsToString());
            Console.WriteLine("LINT globals: " + configuration.GlobalsToString());
            Console.WriteLine("Sharplint: ignorestart=" + configuration.IgnoreStart + ", ignoreend=" + configuration.IgnoreEnd);
            Console.WriteLine();

            int fileCount = 0;

            List<JsLintData> allErrors = new List<JsLintData>();
            

            foreach (string file in configuration.GetMatchedFiles(FilePaths))
            {
                fileCount++;
                string javascript = File.ReadAllText(file);
                if (javascript.IndexOf("/*" + configuration.IgnoreFile + "*/") >= 0)
                {
                    continue;
                }
                lint.Javascript = javascript;
                JsLintResult result = lint.Lint(configuration);
                bool hasErrors = result.Errors.Count > 0;

                if (hasErrors)
                {
                    foreach (JsLintData error in result.Errors)
                    {
                        error.FilePath = file;
                        allErrors.Add(error);
                    }
                    
                    SummaryInfo.Add(String.Format("{0}(0): Lint found {1} errors.", file, result.Errors.Count));
                }
                else
                {
                    SummaryInfo.Add(String.Format("{0}(0): Lint found no errors.", file));
                }

                SharpCompressor compressor = new SharpCompressor();
                if (YUIValidation)
                {
                    compressor.Clear();
                    compressor.AllowEval = configuration.GetOption<bool>("evil");
                    compressor.KeepHeader = packKeepHeader;
                    compressor.CompressorType = compressorType;

                    hasErrors = compressor.YUITest(lint.Javascript);
                    
                    if (hasErrors)
                    {
                 
                        allErrors.AddRange(compressor.Errors);
                        SummaryInfo.Add(String.Format("{0}(0): YUI compressor found {1} errors.", file, compressor.ErrorCount));
                    }
                    else
                    {
                        SummaryInfo.Add(String.Format("{0}(0): YUI compressor found no errors.", file));
                    }
                }

                if (minimizeOutput) {
                    compressor.Clear();
                    compressor.Input = lint.Javascript;

                    string target = MapFileName(file, minimizeMask);
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

            }


            // Output file-by-file results at beginning
            foreach (string item in SummaryInfo)
            {
                Console.WriteLine(item);
            }
            

            if (allErrors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Error Details:");
                Console.WriteLine();

                foreach (JsLintData error in allErrors)
                {
                    Console.WriteLine(string.Format("{0}({1}): ({2}) {3} at character {4}", error.FilePath, error.Line, error.Source, error.Reason, error.Character));
                }
            }
            Console.WriteLine();
            Console.WriteLine("Finished processing at {0:MM/dd/yy H:mm:ss zzz}. Processed {1} files.", DateTime.Now, fileCount);
            
            exit:

            if (readKey)
            {
                Console.ReadKey();
            }
        }

        
        private static string MapFileName(string path, string mask) {
            if (mask.OccurrencesOf("*")!=1)
            {
                throw new Exception("Invalid mask '" + mask + "' for compressing output. It must have a single wildcard.");
            }

            string maskStart = mask.Before("*");
            string maskEnd = mask.AfterLast("*").BeforeLast(".");
            string maskExt = mask.AfterLast(".");

            string pathBase = path.BeforeLast(".");
            return maskStart + pathBase + maskEnd + "." + maskExt;

        }

        private static int LintDataComparer(JsLintData x, JsLintData y) 
        {
            return x.Line.CompareTo(y.Line);
        }
    }
    
}
