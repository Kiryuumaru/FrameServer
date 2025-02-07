using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events;

public class FrameSourceRemovedEventArgs(string key, FrameSourceConfig oldConfig) : EventArgs
{
    public string Key { get; } = key;

    public FrameSourceConfig OldConfig { get; } = oldConfig;
}
