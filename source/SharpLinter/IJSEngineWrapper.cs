using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JTC.SharpLinter
{
    public interface IJSEngineWrapper
    {
        object Run(string code);
        void SetParameter(string name, object value);
    }
}
