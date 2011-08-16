using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JTC.SharpLinter
{
	/// <summary>
	///  Represents a piece of data from the JsLint compiler.
	/// </summary>
	/// <remarks>
	
	/// </remarks>
    public class JsLintData
    {
        public string FilePath { get; set; }
		/// <summary>
		///  The character number on the line this data relates to
		/// </summary>
        public int Character { get; set; }

		/// <summary>
		///  The line number this relates to
		/// </summary>
        public int Line { get; set; }

		/// <summary>
		///  The error or information text
		/// </summary>
        public string Reason { get; set; }

		/// <summary>
		///  The error type
		/// </summary>
        public JsLintDataType ErrorType { get; set; }
        /// <summary>
        /// Who is reporting the error
        /// </summary>
        public string Source
        {
            get;
            set;

        }
        public bool Complete
        {
            get
            {
                return !String.IsNullOrEmpty(Reason);
            }
        }
    }
}
