using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JTC.SharpLinter
{
	/// <summary>
	///  Represents the result of linting some javascript
	/// </summary>
    public class JsLintResult
    {
        public List<JsLintData> Errors { get; set; }
    }
}
