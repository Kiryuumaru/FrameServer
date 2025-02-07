using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

public class FrameSourceConfig : IEquatable<FrameSourceConfig?>
{
    public required string Source { get; set; }

    public required int Port { get; set; }

    public int? Height { get; set; }

    public int? Width { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FrameSourceConfig);
    }

    public bool Equals(FrameSourceConfig? other)
    {
        return other is not null &&
               Source == other.Source &&
               Port == other.Port &&
               Height == other.Height &&
               Width == other.Width;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Port, Height, Width);
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
