using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using JTC.SharpLinter;
using JTC.SharpLinter.Config;

namespace JTC.SharpLinter.Test
{
	[TestClass]
	public class ConfigurationTests
	{
		[TestMethod]
		public void Config()
		{

            JsLintConfiguration config = new JsLintConfiguration();

            Assert.AreEqual("", config.OptionsToString(), "Default has no options set.");
            Assert.AreEqual(String.Empty, config.GlobalsToString(), "Default has no globals set.");
            Assert.AreEqual(100, config.MaxErrors, "Maxerrors=100");
            Assert.AreEqual(true, config.ErrorOnUnused, "Unused default");
            Assert.AreEqual(config.MaxErrors,config.GetOption<int>("maxerr") , config.MaxErrors, "Maxerrors = options(maxerr)");
            
            config.SetOption("unused");
            config.SetOption("evil");
            config.SetOption("maxerr", 50);
            config.SetOption("immed");

            Assert.AreEqual("unused: true, evil: true, maxerr: 50, immed: true", config.OptionsToString(), "Basic option setting worked.");

            JsLintConfiguration config2 = new JsLintConfiguration();
            config2.SetOption("eqeqeq", true);
            config2.SetOption("maxerr", 25);
            config2.SetOption("immed", false);

            config.MergeOptions(config2);

            Assert.AreEqual("unused: true, evil: true, maxerr: 25, immed: false, eqeqeq: true", config.OptionsToString(), "Basic option setting worked.");

        }
        [TestMethod]
        public void ConfigFile()
        {
            string configFile = "D:\\VSProjects\\SharpLinter\\source\\SharpLinterExe\\bin\\Debug\\jslint.test.conf";
            
            JsLintConfiguration config = new JsLintConfiguration();

            config.MergeOptions(configFile);

            Assert.AreEqual("browser: false, nomen: false, plusplus: false, forin: false, windows: true, laxbreak: true", config.OptionsToString(),"Got lint options from conf file");

            Assert.AreEqual("jQuery: true, HTMLElement: true, $: true", config.GlobalsToString());
            Assert.AreEqual(config.ExcludeFiles.Count, 2, "Got 2 files");
            Assert.AreEqual("*.min.js", config.ExcludeFiles.ElementAt(0), "First file was right");
        }
	}
}
