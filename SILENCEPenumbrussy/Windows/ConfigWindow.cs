using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace SilencePenumbrussy.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("SILENCEPenumbrussy Settings###SILENCEPenumbrussyConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(420, 270);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Penumbra mod-complete notifications");
        ImGui.Spacing();

        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable filtering", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

        ImGui.BeginDisabled(!configuration.Enabled);
        var suppress = configuration.SuppressModComplete;
        if (ImGui.Checkbox("Suppress immediately", ref suppress))
        {
            configuration.SuppressModComplete = suppress;
            configuration.Save();
        }

        var autoDismiss = configuration.AutoDismissModComplete;
        if (ImGui.Checkbox("Auto-dismiss after", ref autoDismiss))
        {
            configuration.AutoDismissModComplete = autoDismiss;
            configuration.Save();
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!configuration.AutoDismissModComplete);
        ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
        var seconds = configuration.AutoDismissSeconds;
        if (ImGui.SliderInt("##AutoDismissSeconds", ref seconds, 1, 60, "%d s"))
        {
            configuration.AutoDismissSeconds = seconds;
            configuration.Save();
        }
        ImGui.EndDisabled();

        var matchSource = configuration.MatchPenumbraSource;
        if (ImGui.Checkbox("Match Penumbra source only", ref matchSource))
        {
            configuration.MatchPenumbraSource = matchSource;
            configuration.Save();
        }

        var strictMatch = configuration.StrictTextMatch;
        if (ImGui.Checkbox("Require both import and extract text", ref strictMatch))
        {
            configuration.StrictTextMatch = strictMatch;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Tip: if Penumbra changes the notification text, relax the text match or disable source matching.");
        ImGui.EndDisabled();

        ImGui.Spacing();
        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }
    }
}
