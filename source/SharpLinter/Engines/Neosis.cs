using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Noesis.Javascript;

namespace JTC.SharpLinter.Engines
{
    public class Neosis: IJSEngineWrapper
    {
        public Neosis()
        {

            Context = new JavascriptContext();
        }
        protected JavascriptContext Context;
        public object Run(string code)
        {
            return Context.Run(code);
        }

        public void SetParameter(string name, object value)
        {
            Context.SetParameter(name,value);
        }
    }
}
