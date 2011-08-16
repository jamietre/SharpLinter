using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JTC.SharpLinter.Config
{
    public class PathInfo
    {
        public PathInfo(string path, bool recurse)
        {
            Path = path;
            Recurse = recurse;
        }
        public string Path { get; set; }
        public bool Recurse { get; set; }
    }
}
