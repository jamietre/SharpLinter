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

        static int Main(string[] args)
        {
           
            bool readKey = false;
            int errorCount = 0;
            try
            {
                Lazy<JsLintConfiguration> _Configuration = new Lazy<JsLintConfiguration>();
                Func<JsLintConfiguration> Configuration = () => { return _Configuration.Value; };

                if (args.Length == 0)
                {
                    Console.WriteLine("SharpLinter [-[r]f /path/*.js] [-o options] ");
                    Console.WriteLine("            [-c sharplinter.conf] [-j jslint.js] [-y]");
                    Console.WriteLine("            [-v[1|2|3]] [--noglobal]");
                    Console.WriteLine("            [-p[h] yui|packer|best mask] [-k] ");
                    Console.WriteLine("            [-i ignore-start ignore-end] [-if text] [-of \"format\"] [file]");
                    Console.WriteLine();
                    Console.WriteLine(("Options: \n\n" +
                                    "-[r]f c:\\scripts\\*.js     parse all files matching \"*.js\" in \"c:\\scripts\"\n" +
                                    "                          if called with \"r\", will recurse subfolders\n" +
                                    "-o \"option option ...\"    set jslint/jshint options specified, separated by\n" +
                                    "                          spaces, in format \"option\" or \"option: true|false\"\n" +
                                    "-v[1|2|3]                 be [terse][verbose-default][really verbose]\n" +
                                    "\n" +
                                    "-k                        Wait for a keytroke when done\n" +
                                    "-c c:\\sharplinter.conf   load config options from file specified\n" +
                                    "--noglobal                ignore global config file\n" +
                                    "-j jslint.js              use file specified to parse files instead of embedded\n" +
                                    "                          (probably old) script\n" +
                                    "-y                        Also run the script through YUI compressor to look\n" +
                                    "                          forerrors\n" +
                                    "\n" +
                                    "-i text-start text-end    Ignore blocks bounded by /*text-start*/ and\n" +
                                    "                          /*text-end*/\n" +
                                    "-if text-skip             Ignore files that contain /*text-skip*/ anywhere\n" +
                                    "-of \"output format\"       Use the string as a format for the error output. The\n" +
                                    "                          default is:\n" +
                                    "                          \"{0}({1}): ({2}) {3} at character {4}\". The parms are\n" +
                                    "                          {0}: full file path, {1}: line number, {2}: source\n" +
                                    "                          (lint or yui), {4}: character\n" +
                                    "\n" +
                                    "-p[h] yui|packer|best *.min.js      Pack/minimize valid input using YUI\n" +
                                    "                                    Compressor, Dean Edwards' JS Packer, or \n" +
                                    "                          whichever produces the smallest file. Output to a\n" +
                                    "                          file \"filename.min.js\". If validation fails, \n" +
                                    "                          the output file will be deleted (if exists)\n" +
                                    "                          to ensure no version mismatch. If  -h is specified,\n" +
                                    "                          the first comment block in the file /* ... */\n" +
                                    "                          will be passed uncompressed at the beginning of the\n" +
                                    "                          output.\n")
                                    .Replace("\n", Environment.NewLine));




                    Console.Write("Options Format:");
                    Console.WriteLine(JsLintConfiguration.GetParseOptions());

                    Console.WriteLine();
                    Console.WriteLine("E.g.");
                    Console.WriteLine("JsLint -f input.js -f input2.js");
                    Console.WriteLine("JsLint -f input.js -o \"evil=False,eqeqeq,predef=Microsoft System\"");
                    return 0;
                }

                //string commandlineConfig = String.Empty;
                string commandLineOptions = String.Empty;
                string globalConfigFile = "";
                string excludeFiles = "";
                string jsLintSource = "";

                HashSet<PathInfo> filePaths = new HashSet<PathInfo>();

 
                bool recurse = false;
                bool noGlobal = false;

                LinterType linterType = 0;

                CompressorType compressorType = 0;

                JsLintConfiguration finalConfig = new JsLintConfiguration();


                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].Trim().ToLower();
                    string value = args.Length > i + 1 ? args[i + 1] : String.Empty;
                    string value2 = args.Length > i + 2 ? args[i + 2] : String.Empty;
                    //string filter = null;
                    switch (arg)
                    {
                        case "-of":
                            finalConfig.OutputFormat = value.Replace("\\r", "\r").Replace("\\n", "\n");
                            i++;
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
                            if (arg == "-ph")
                            {
                                finalConfig.MinimizeKeepHeader = true;
                            }
                            finalConfig.CompressorType = compressorType;
                            i += 2;
                            break;
                        case "-y":
                            finalConfig.YUIValidation = true;
                            break;
                        case "-c":
                            globalConfigFile = value;
                            i++;
                            break;
                        case "-j":

                            if (File.Exists(value))
                            {
                                jsLintSource = value;
                            }
                            else
                            {
                                Console.WriteLine(String.Format("Cannot find JSLint source file {0}", value));
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
                            excludeFiles = excludeFiles.AddListItem(value, " ");
                            i++;
                            break;
                        case "-v":
                        case "-v1":
                        case "-v2":
                        case "-v3":
                            finalConfig.Verbosity = arg.Length == 2 ? Verbosity.Debugging :
                                (Verbosity)Convert.ToInt32(arg.Substring(2, 1));

                            break;
                        case "--noglobal":
                            noGlobal = true;
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
                // Done parsing options.. look for linter

                string lintSourcePath = "";
                string lintSource = GetLinter(jsLintSource, out lintSourcePath);
                if (!string.IsNullOrEmpty(lintSource))
                {
                    finalConfig.JsLintCode = lintSource;
                    finalConfig.JsLintFilePath = lintSourcePath;
                }

                if (!string.IsNullOrEmpty(excludeFiles))
                {
                    foreach (string file in excludeFiles.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        finalConfig.SetFileExclude(file);
                    }
                }

                // Get global config options
                if (!noGlobal)
                {
                    string globalConfigPath;
                    string config = GetConfig(globalConfigFile, out globalConfigPath);
                    if (!string.IsNullOrEmpty(config))
                    {
                        try
                        {
                            finalConfig.MergeOptions(JsLintConfiguration.ParseConfigFile(config, linterType));
                            finalConfig.GlobalConfigFilePath = globalConfigPath;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            goto exit;
                        }
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
                    errorCount = batch.Process();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Parser error: " + e.Message +
                        (finalConfig.Verbosity == Verbosity.Debugging ?
                            ". Stack trace (verbose mode): " + e.StackTrace :
                            ""));
                    goto exit;
                }

            }
            catch(Exception e) {
                Console.WriteLine(e.Message);

            }

            exit:
            if (readKey)
            {
                Console.ReadKey();
            }

            return errorCount == 0 ? 0 : 1;
        }

        #region support functions
        /// <summary>
        /// Get a linter, either from default locations, embedded file or user passed
        /// </summary>
        /// <param name="jsLintSource"></param>
        private static string GetLinter(string jsLintSource, out string path)
        {

            // bool parm means fail if missing (only for user-specified source)
            List<Tuple<string, bool>> sources = new List<Tuple<string, bool>>();
            if (!String.IsNullOrEmpty(jsLintSource))
            {
                sources.Add(Tuple.Create<string, bool>(Utility.ResolveRelativePath_AppRoot(jsLintSource), true));
            }
            sources.Add(Tuple.Create<string, bool>(Utility.ResolveRelativePath_AppRoot("jslint.js"), false));
            sources.Add(Tuple.Create<string, bool>(Utility.ResolveRelativePath_AppRoot("jshint.js"), false));

            // also check exe location
            string exeLocation = Assembly.GetAssembly(typeof(JTC.SharpLinter.SharpLinterExe)).Location;
            sources.Add(Tuple.Create<string, bool>(exeLocation + "\\jslint.js", false));
            sources.Add(Tuple.Create<string, bool>(exeLocation + "\\jshint.js", false));
            return GetFirstMatchingFile(sources, out path);

        }
        private static string GetConfig(string globalConfigFile, out string path)
        {
            //
            List<Tuple<string, bool>> sources = new List<Tuple<string, bool>>();
            if (!String.IsNullOrEmpty(globalConfigFile))
            {
                sources.Add(Tuple.Create<string, bool>(Utility.ResolveRelativePath_AppRoot(globalConfigFile), true));
            }
            sources.Add(Tuple.Create<string,bool>(Utility.ResolveRelativePath_AppRoot("sharplinter.conf"),false));
            sources.Add(Tuple.Create<string,bool>(Assembly.GetAssembly(typeof(JTC.SharpLinter.SharpLinterExe)).Location+"\\sharplinter.conf",false));

            return GetFirstMatchingFile(sources, out path);
        }
        private static string GetFirstMatchingFile(IEnumerable<Tuple<string, bool>> files, out string path)
        {

            string fileText = null;
            path = "";
            foreach (var item in files)
            {
                if (File.Exists(item.Item1))
                {
                    try
                    {
                        fileText = File.ReadAllText(item.Item1);
                        path = item.Item1;
                        break;
                    }
                    catch
                    {
                        Console.WriteLine(String.Format("The file \"{0}\" appears to be invalid.", item.Item1));
                        path = "";
                        break;
                    }

                }
                else if (item.Item2) // was required
                {
                    Console.WriteLine(String.Format("The file \"{0}\" does not exist.", item.Item1));
                    
                }
            }
            return fileText;

        }
        private static int LintDataComparer(JsLintData x, JsLintData y) 
        {
            return x.Line.CompareTo(y.Line);
        }
        #endregion
    }
    
}
