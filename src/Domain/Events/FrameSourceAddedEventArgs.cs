using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events;

public class FrameSourceAddedEventArgs(string key, FrameSourceConfig newConfig) : EventArgs
{
    public string Key { get; } = key;

    public FrameSourceConfig NewConfig { get; } = newConfig;
}
