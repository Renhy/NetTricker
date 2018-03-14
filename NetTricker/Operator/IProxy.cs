using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTricker.Operator
{
    interface IProxy
    {
        string Type { get; }

        bool IsProxy { get; }

        bool Proxy();

        bool UnProxy();

    }
}
