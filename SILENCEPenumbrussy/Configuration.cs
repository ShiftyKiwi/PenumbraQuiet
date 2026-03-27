using Dalamud.Configuration;
using System;

namespace PenumbraQuiet;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;
    public bool SuppressModComplete { get; set; } = true;
    public bool MatchPenumbraSource { get; set; } = true;
    public bool StrictTextMatch { get; set; } = true;
    public bool SuppressPenumbraErrorToasts { get; set; } = false;
    public bool RemovePenumbraErrorMessages { get; set; } = false;
    public bool DebugPenumbraMessages { get; set; } = false;
    public bool IsConfigWindowMovable { get; set; } = true;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
