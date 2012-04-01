using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using JTC.SharpLinter;
using JTC.SharpLinter.Config;

namespace JTC.SharpLinter.Test
{
    class TestResources
    {
        public static string GetAppRootedPath(string relativePath)
        {
            string exePath = Assembly.GetAssembly(typeof(JTC.SharpLinter.Test.TestResources)).Location;
            string rootFolder = "SharpLinter.Test";
            int rootPos = exePath.IndexOf(rootFolder + "\\") + rootFolder.Length + 1;
            if (relativePath.Length > 0 && relativePath[0] == '\\')
            {
                relativePath = relativePath.Substring(1);
            }
            return exePath.Substring(0,rootPos) + relativePath.Replace("/", "\\");
                
        }
        public static string LoadAppRootedFile(string relativePath) {
            return File.ReadAllText(GetAppRootedPath(relativePath));
        }
        public static  JsLintConfiguration DefaultConfig
        {
            get
            {
                var config = new JsLintConfiguration();
                config.SetOption("browser");
                config.SetOption("bitwise");
                config.SetOption("evil");
                config.SetOption("eqeqeq");
                config.SetOption("plusplus");
                config.SetOption("forin");
                config.SetOption("immed");
                config.SetOption("newcap");
                config.SetOption("undef");
                return config;

            }
        }
    }
}
