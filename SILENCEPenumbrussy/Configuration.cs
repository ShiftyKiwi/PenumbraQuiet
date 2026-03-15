using Dalamud.Configuration;
using System;

namespace SilencePenumbrussy;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;
    public bool SuppressModComplete { get; set; } = true;
    public bool AutoDismissModComplete { get; set; } = false;
    public int AutoDismissSeconds { get; set; } = 5;
    public bool MatchPenumbraSource { get; set; } = true;
    public bool StrictTextMatch { get; set; } = true;
    public bool IsConfigWindowMovable { get; set; } = true;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
