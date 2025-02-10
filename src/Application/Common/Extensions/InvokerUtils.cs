using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Extensions;

public static class InvokerUtils
{
    public static void RunAndForget(Action action)
    {
        try
        {
            action();
        }
        catch { }
    }
}
