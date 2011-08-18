using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using JTC.SharpLinter;
using JTC.SharpLinter.Config;

namespace JTC.SharpLinter.Test
{
	[TestClass]
	public class LinterUnitTests
	{
        protected JsLintConfiguration DefaultConfig
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

		[TestMethod]
		public void TestMultipleCalls()
		{

            var config = DefaultConfig;
            
            SharpLinter lint = new SharpLinter(config);
            
            
            JsLintResult result = lint.Lint(
							@"var i, y; for (i = 0; i < 5; i++) console.Print(message + ' (' + i + ')'); number += i;");

            // original test was 4 errors - jshint defaults?
			Assert.AreEqual(4, result.Errors.Count);

			JsLintResult result2 = lint.Lint(
                            @"function annon() { var i, number; for (i = 0; i === 5; i++) { number += i; } }");

			Assert.AreEqual(1, result2.Errors.Count);

            JsLintResult result3 = lint.Lint(
                            @"function annon() { var i, number, x; for (i = 0; i == 5; i++) { number += i; } }");

			Assert.AreEqual(2, result3.Errors.Count);
		}

		[TestMethod]
		public void TestMultipleDifferentOptions()
		{
            JsLintConfiguration config = new JsLintConfiguration();
			SharpLinter lint = new SharpLinter(config);
            
            config.SetOption("eqeqeq",true);
            config.SetOption("plusplus",true);
            
            JsLintResult result = lint.Lint(
							@"function annon() { var i, number, x; for (i = 0; i == 5; i++) { number += ++i; } }"							
                            );

			Assert.AreEqual(3, result.Errors.Count);

            config = new JsLintConfiguration();
            config.SetOption("unused", false);

            // should fail on ++ since "plusplus=true" -- note that we are testing with JSHINT so the
            // behavior is opposite of JSLINT for this options

			JsLintResult result2 = lint.Lint(
							@"function annon() { var i, number; for (i = 0; i === 5; i++) { number += i; } }");

			Assert.AreEqual(1, result2.Errors.Count);
		}

		[TestMethod]
		public void TestArgumentParsing()
		{
            JsLintConfiguration config = JsLintConfiguration.ParseString("  maxerr : 2,eqeqeq,unused :    TRUE,predef : test1 TEST2   3 ,evil:false , browser : true", LinterType.JSHint);

			Assert.AreEqual(2, config.MaxErrors);
            Assert.AreEqual("maxerr: 2, eqeqeq: true, unused: true, evil: false, browser: true", config.OptionsToString());
			Assert.AreEqual(true, config.ErrorOnUnused);
			Assert.AreEqual(3, config.Globals.Count());
			Assert.AreEqual("test1", config.Globals.ElementAt(0));
            Assert.AreEqual("TEST2", config.Globals.ElementAt(1));
			Assert.AreEqual("3", config.Globals.ElementAt(2));
		}

		[TestMethod]
		public void TestArgumentParsing2()
		{
			JsLintConfiguration config = JsLintConfiguration.ParseString("  maxerr : 400,eqeqeq : true,unused :    FALSE,predef : 1 window alert,evil:true , browser : false",LinterType.JSHint);

			Assert.AreEqual(400, config.MaxErrors);
            Assert.AreEqual("maxerr: 400, eqeqeq: true, unused: false, evil: true, browser: false", config.OptionsToString());
			Assert.AreEqual(false, config.ErrorOnUnused);
			Assert.AreEqual(3, config.Globals.Count());
			Assert.AreEqual("1", config.Globals.ElementAt(0));
		}

	}
}
