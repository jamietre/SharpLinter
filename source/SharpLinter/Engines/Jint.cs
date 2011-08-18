using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jint;

namespace JTC.SharpLinter.Engines
{
    
    public class Jint : IJSEngineWrapper
    {
        public Jint()
        {
            JintEngine = new JintEngine();
        }
        protected JintEngine JintEngine;
        public object Run(string code)
        {
            return JintEngine.Run(code);
        }

        public void SetParameter(string name, object value)
        {
            JintEngine.SetParameter(name, value);
        }
    }
    
}
