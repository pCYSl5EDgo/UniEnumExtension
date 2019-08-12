using System;
using System.Collections.Generic;

namespace UniEnumExtension
{
    public interface IExtender : IDisposable
    {
        void Extend(IEnumerable<string> assemblyPaths);
    }
}
