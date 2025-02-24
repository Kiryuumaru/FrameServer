using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Configuration.Models;

public class FrameServerConfigFile
{
    public Dictionary<string, FrameSourceConfig> Sources { get; set; } = [];
}
