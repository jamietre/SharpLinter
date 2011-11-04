using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JTC.SharpLinter;
using JTC.SharpLinter.Config;
using System.Reflection;

/*
 * 
 * SharpLinter
 * (c) 2011 James Treworgy
 * 
 * Based on code originally created by Luke Page/ScottLogic: http://www.scottlogic.co.uk/2010/09/js-lint-in-visual-studio-part-1/
 * 
 */
namespace JTC.SharpLinter
{

    class SharpLinterExe
    {

        static void Main(string[] args)
        {
            Lazy<JsLintConfiguration> _Configuration = new Lazy<JsLintConfiguration>();
            Func<JsLintConfiguration> Configuration = () => { return _Configuration.Value; };

			if (args.Length == 0)
			{
				Console.WriteLine("SharpLinter [-[r]f /path/*.js] [-o options] [-v] ");
                Console.WriteLine("            [-c sharplinter.conf] [-j jslint.js] [-y]");
                Console.WriteLine("            [-p[h] yui|packer|best mask] [-k] ");
                Console.WriteLine("            [-i ignore-start ignore-end] [-if text] [-of \"format\"]");
				Console.WriteLine();
                Console.WriteLine(("Options: \n\n" +
                                "-[r]f c:\\scripts\\*.js     parse all files matching \"*.js\" in \"c:\\scripts\"\n" +
                                "                          if called with \"r\", will recurse subfolders\n" +
                                "-o \"option option ...\"    set jslint/jshint options specified, separated by\n"+
                                "                          spaces, in format \"option\" or \"option: true|false\"\n" +
                                "-v                        be verbose (report information other than errors)\n" +
                                "\n" +
                                "-k                        Wait for a keytroke when done\n" +
                                "-c c:\\sharplinter.conf    load config options from file specified\n" +
                                "-j jslint.js              use file specified to parse files instead of embedded\n"+
                                "                          (probably old) script\n" +
                                "-y                        Also run the script through YUI compressor to look\n"+
                                "                          forerrors\n" +
                                "\n"+
                                "-i text-start text-end    Ignore blocks bounded by /*text-start*/ and\n"+
                                "                          /*text-end*/\n" +
                                "-if text-skip             Ignore files that contain /*text-skip*/ anywhere\n" +
                                "-of \"output format\"       Use the string as a format for the error output. The\n"+
                                "                          default is:\n" +
                                "                          \"{0}({1}): ({2}) {3} at character {4}\". The parms are\n" +
                                "                          {0}: full file path, {1}: line number, {2}: source\n" +
                                "                          (lint or yui), {4}: character\n" +
                                "\n" +
                                "-p[h] yui|packer|best *.min.js      Pack/minimize valid input using YUI\n"+
                                "                                    Compressor, Dean Edwards' JS Packer, or \n" +
                                "                          whichever produces the smallest file. Output to a\n" +
                                "                          file \"filename.min.js\". If validation fails, \n" +
                                "                          the output file will be deleted (if exists)\n" +
                                "                          to ensure no version mismatch. If  -h is specified,\n" +
                                "                          the first comment block in the file /* ... */\n" +
                                "                          will be passed uncompressed at the beginning of the\n"+
                                "                          output.\n")
                                .Replace("\n", Environment.NewLine));


                                
                
                Console.Write("Options Format:");
				Console.WriteLine(JsLintConfiguration.GetParseOptions());
                
				Console.WriteLine();
				Console.WriteLine("E.g.");
				Console.WriteLine("JsLint -f input.js -f input2.js");
				Console.WriteLine("JsLint -f input.js -o \"evil=False,eqeqeq,predef=Microsoft System\"");
				return;
			}

            //string commandlineConfig = String.Empty;
            string commandLineOptions = String.Empty;
            string globalConfigFile = "jslint.conf";
            string excludeFiles = String.Empty;
            string jsLintSource = "jslint.js";

            HashSet<PathInfo> filePaths = new HashSet<PathInfo>();

            bool readKey = false;
            bool recurse  = false;

            LinterType linterType = 0;

            CompressorType compressorType = 0;
            
            JsLintConfiguration finalConfig = new JsLintConfiguration();


			for (int i = 0; i < args.Length; i++ )
			{
				string arg = args[i].Trim().ToLower();
                string value = args.Length > i+1 ? args[i + 1] : String.Empty;
                string value2 = args.Length > i+2 ? args[i + 2] : String.Empty;
				//string filter = null;
				switch (arg)
				{
                    case "-of":
                        finalConfig.OutputFormat = value.Replace("\\r", "\r").Replace("\\n", "\n");
                        break;
                    case "-i":
                        finalConfig.IgnoreStart = value;
                        finalConfig.IgnoreFile = value2;
                        break;
                    case "-ie":
                        finalConfig.IgnoreEnd = value;
                        break;
                    case "-p":
                    case "-ph":

                        if (!Enum.TryParse<CompressorType>(value, out compressorType))
                        {
                            Console.WriteLine(String.Format("Unknown pack option {0}", value));
                            goto exit;
                        }
                        finalConfig.MinimizeOnSuccess = true;
                        finalConfig.MinimizeFilenameMask = value2;
                        if (arg=="-ph") {
                            finalConfig.MinimizeKeepHeader = true;
                        }
                        finalConfig.CompressorType = compressorType;
                        i+=2;
                        break;
                    case "-y":
                        finalConfig.YUIValidation = true;
                        break;
                    case "-c":
                        globalConfigFile = value;
                        i++;
						break;
                    case "-j":

                        if (File.Exists(value)) {
                            jsLintSource = value;
                        } else {
                            Console.WriteLine(String.Format("Cannot find JSLint source file {0}",value));
                            goto exit;
                        }
                        i++;
                        break;
                    case "-k":
                        readKey = true;
                        break;
					case "-f":
					case "-rf":
                        filePaths.Add(new PathInfo(value, arg == "-rf"));
                        i++;
						break;
                    case "-r":
                        recurse = true;
                        break;
					case "-o":
                        commandLineOptions = commandLineOptions.AddListItem(value, " ");
                        i++;
						break;
                    case "-x":
                       excludeFiles = excludeFiles.AddListItem(value," ");
                        i++;
                        break;
                    case "-v":
                        finalConfig.Verbose = true;
                        break;
                    default:
                        if (arg[0] == '-')
                        {
                            throw new Exception("Unrecognized command line option \"" + arg + "\"");
                        }
                        filePaths.Add(new PathInfo(arg, recurse));
                        break;
				}
			}
            // Done parsing options

            if (!String.IsNullOrEmpty(jsLintSource))
            {
                jsLintSource = Utility.ResolveRelativePath_AppRoot(jsLintSource);
                try
                {
                    finalConfig.JsLintCode = File.ReadAllText(jsLintSource);
                }
                catch
                {
                    Console.WriteLine(String.Format("The JSLINT/JSHINT file \"{0}\" appears to be invalid.", jsLintSource));
                    goto exit;
                }
            }
            else
            {
                // See if there's a default linter

                string defaultLint = Path.GetDirectoryName(Assembly.GetAssembly(typeof(JTC.SharpLinter.SharpLinterExe)).Location) + "\\jslint.js";
                
                if (File.Exists(defaultLint))
                {
                    finalConfig.JsLintCode = File.ReadAllText(defaultLint);
                }
            }

            if (!string.IsNullOrEmpty(excludeFiles))
            {
                foreach (string file in excludeFiles.Split(new char[] {' '},StringSplitOptions.RemoveEmptyEntries))
                {
                    finalConfig.SetFileExclude(file);
                }

            }
            // Get global config first.
            if (!String.IsNullOrEmpty(globalConfigFile))
            {
                globalConfigFile = Utility.ResolveRelativePath_AppRoot(globalConfigFile);
                if (File.Exists(globalConfigFile))
                {
                    try
                    {
                        finalConfig.MergeOptions(JsLintConfiguration.ParseConfigFile(File.ReadAllText(globalConfigFile), linterType));
                        finalConfig.GlobalConfigFile = globalConfigFile;
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

            // add the basic config we built so far
            finalConfig.MergeOptions(Configuration());

            // Overlay any command line options
            if (commandLineOptions != null)
            {
                try
                {
                   finalConfig.MergeOptions(JsLintConfiguration.ParseString(commandLineOptions, linterType));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    goto exit;
                }

            }
            
            try
            {
                SharpLinterBatch batch = new SharpLinterBatch(finalConfig);
                batch.FilePaths = filePaths;
                batch.Process();
            }
            catch(Exception e)
            {
                Console.WriteLine("Everything was looking good on your command line, but the parser threw an error: "+ e.Message +" , " + e.StackTrace );
                goto exit;
            }
            


            exit:
            if (readKey)
            {
                Console.ReadKey();
            }
        }


        


        private static int LintDataComparer(JsLintData x, JsLintData y) 
        {
            return x.Line.CompareTo(y.Line);
        }
    }
    
}
