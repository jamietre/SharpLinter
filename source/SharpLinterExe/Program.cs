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
				Console.WriteLine("SharpLinter -i file.js -[r]d /directory [-c sharplinter.conf] [-o options] [-j jslint.js] [-y] [-p yui | packer | best] [-ph] [-k] ");
				Console.WriteLine();
				Console.Write("Options Format:");
				Console.WriteLine(JsLintConfiguration.GetParseOptions());
				Console.WriteLine();
				Console.WriteLine("E.g.");
				Console.WriteLine("JsLint -i input.js -i input2.js");
				Console.WriteLine("JsLint -i input.js -o \"evil=False,eqeqeq,predef=Microsoft System\"");
				return;
			}

            JsLintConfiguration commandlineConfig = new JsLintConfiguration();
            JsLintConfiguration globalConfig = null;

            string jsLintSource = String.Empty;
            HashSet<PathInfo> FilePaths = new HashSet<PathInfo>();

            bool YUIValidation = false;
            bool readKey = false;
            bool minimizeOutput = false;
            bool packKeepHeader = false;
            string minimizeMask = String.Empty;

            CompressorType compressorType = 0;
            
			for (int i = 0; i < args.Length; i++ )
			{
				string arg = args[i].Trim().ToLower();
                string value = args.Length > i+1 ? args[i + 1] : String.Empty;
                string value2 = args.Length > i+2 ? args[i + 2] : String.Empty;
				//string filter = null;
				switch (arg)
				{
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
                        if (File.Exists(value))
						{
                            try
                            {
                                globalConfig = JsLintConfiguration.ParseConfigFile(File.ReadAllText(value));
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e.Message);
                                return;
                            }
						}
						else
						{
							Console.WriteLine(String.Format("Cannot find global configuration file {0}", value));
							return;
						}
                        i++;
						break;
					case "-i":
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
						try
						{
                            commandlineConfig.MergeOptions(JsLintConfiguration.ParseString(value));
						}
						catch (Exception e)
						{
							Console.WriteLine(e.Message);
							return;
						}
                        i++;
						break;
                    case "-x":
                        commandlineConfig.SetFileExclude(value);
                        i++;
                        break;
				}
			}

            JsLintConfiguration configuration = new JsLintConfiguration();
            if (globalConfig != null)
            {
                configuration.MergeOptions(globalConfig);
            }
            if (commandlineConfig != null)
            {
                configuration.MergeOptions(commandlineConfig);
            }

            if (configuration == null)
            {
                configuration = new JsLintConfiguration();
            }

            SharpLinter lint = new SharpLinter(jsLintSource);

            // collect details to output at the end
            List<string> SummaryInfo = new List<string>();

            Console.WriteLine("Beginning processing at {0:MM/dd/yy H:mm:ss zzz}",DateTime.Now);
            Console.WriteLine("LINT options: " + configuration.OptionsToString());
            Console.WriteLine("LINT globals: " + configuration.GlobalsToString());

            int fileCount = 0;

            List<JsLintData> allErrors = new List<JsLintData>();

            foreach (string file in configuration.GetMatchedFiles(FilePaths))
            {
                fileCount++;
                lint.TargetPath = file;
                JsLintResult result = lint.Lint(configuration);
                bool hasErrors = result.Errors.Count > 0;

                if (hasErrors)
                {
                    allErrors.AddRange(result.Errors);
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
                        if (compressor.Minimize())
                        {
                            if (File.Exists(target))
                            {
                                File.Delete(target);
                            }
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
                foreach (JsLintData error in allErrors)
                {
                    Console.WriteLine(string.Format("{0}({1}): ({2}) {3} at character {4}", file, error.Line, error.Source,error.Reason, error.Character));
                }
            }


            // Output file-by-file results at end
            foreach (string item in SummaryInfo)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("Finished processing at {0:MM/dd/yy H:mm:ss zzz}. Processed {1} files.", DateTime.Now,fileCount);
            
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
