using System;
using System.Collections.Generic;
using System.Text;

namespace AzDoBoards.Utility;

public class CacheBuster
{
    public string Value { get; }

    public CacheBuster()
    {
        Value = DateTime.Now.ToString("yyyyMMddHHmm");
    }
}
