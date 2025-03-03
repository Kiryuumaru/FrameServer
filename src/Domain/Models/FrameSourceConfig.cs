﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

public class FrameSourceConfig : IEquatable<FrameSourceConfig?>
{
    public required string Source { get; set; }

    public required int Port { get; set; }

    public bool Enabled { get; set; } = true;

    public bool ShowWindow { get; set; } = false;

    public int? Height { get; set; }

    public int? Width { get; set; }

    public int? Fps { get; set; }

    public string? VideoApi { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FrameSourceConfig);
    }

    public bool Equals(FrameSourceConfig? other)
    {
        return other is not null &&
               Source == other.Source &&
               Port == other.Port &&
               Enabled == other.Enabled &&
               ShowWindow == other.ShowWindow &&
               Height == other.Height &&
               Width == other.Width &&
               Fps == other.Fps &&
               VideoApi == other.VideoApi;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Port, Enabled, ShowWindow, Height, Width, Fps, VideoApi);
    }

    public static bool operator ==(FrameSourceConfig? left, FrameSourceConfig? right)
    {
        return EqualityComparer<FrameSourceConfig>.Default.Equals(left, right);
    }

    public static bool operator !=(FrameSourceConfig? left, FrameSourceConfig? right)
    {
        return !(left == right);
    }
}
