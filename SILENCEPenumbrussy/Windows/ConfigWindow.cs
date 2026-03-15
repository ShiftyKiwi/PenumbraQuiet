using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
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

        Size = new Vector2(420, 330);
        SizeCondition = ImGuiCond.FirstUseEver;

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
        if (ImGui.Checkbox("Suppress mod-complete toasts", ref suppress))
        {
            configuration.SuppressModComplete = suppress;
            configuration.Save();
        }

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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Penumbra error notifications");
        ImGui.Spacing();

        var suppressErrorToasts = configuration.SuppressPenumbraErrorToasts;
        if (ImGui.Checkbox("Suppress error toasts", ref suppressErrorToasts))
        {
            configuration.SuppressPenumbraErrorToasts = suppressErrorToasts;
            configuration.Save();
        }

        var removeErrorMessages = configuration.RemovePenumbraErrorMessages;
        if (ImGui.Checkbox("Remove from Penumbra messages tab", ref removeErrorMessages))
        {
            configuration.RemovePenumbraErrorMessages = removeErrorMessages;
            configuration.Save();
        }

        ImGui.TextWrapped("Targets \"Forbidden File Redirection\" and \"Collection without ID found\" messages.");
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
