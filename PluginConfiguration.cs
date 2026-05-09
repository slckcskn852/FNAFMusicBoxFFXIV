using Dalamud.Configuration;
using System.Numerics;

namespace FNAFMusicBoxFFXIV;

public sealed class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public Vector2 WindowPosition { get; set; } = new(100, 100);

    public Vector2 WindowSize { get; set; } = new(430, 160);

    public bool IsVisible { get; set; } = true;

    public bool LockPosition { get; set; }

    public float Opacity { get; set; } = 0.92f;

    public float UiScale { get; set; } = 1.0f;

    // Target vertical resolution for DPI scaling (1080, 1440, 2160)
    public int DpiHeight { get; set; } = 1440;
    public float WindSpeed { get; set; } = 38f;

    public float DecaySpeed { get; set; } = 12f;

    // Added Volume property, defaulting to 50%
    public float Volume { get; set; } = 0.5f; 
}