using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PenumbraQuiet.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("PenumbraQuiet Settings###PenumbraQuietConfig")
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
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.2f, 1f));
        ImGui.TextWrapped("⚠ Troubleshooting Notice\n\nThis plugin can hide some Penumbra error messages and notifications.\n\nIf you run into mod issues, disable PenumbraQuiet and check whether the issue still happens before asking for help in the Penumbra Discord server.\n\nIf the issue goes away with PenumbraQuiet disabled, please report it on this plugin's GitHub repository.");
        ImGui.PopStyleColor();
        ImGui.Spacing();

        ImGui.TextUnformatted("Mod-complete notifications");
        ImGui.Spacing();

        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable filtering", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }

        ImGui.BeginDisabled(!configuration.Enabled);
        var suppress = configuration.SuppressModComplete;
        if (ImGui.Checkbox("Hide mod-complete pop-ups", ref suppress))
        {
            configuration.SuppressModComplete = suppress;
            configuration.Save();
        }

        var matchSource = configuration.MatchPenumbraSource;
        if (ImGui.Checkbox("Only hide notifications from Penumbra", ref matchSource))
        {
            configuration.MatchPenumbraSource = matchSource;
            configuration.Save();
        }

        var strictMatch = configuration.StrictTextMatch;
        if (ImGui.Checkbox("Use stricter text matching", ref strictMatch))
        {
            configuration.StrictTextMatch = strictMatch;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Tip: if Penumbra changes the notification text, make text matching less strict or turn off \"Only hide notifications from Penumbra.\"");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Error notifications");
        ImGui.Spacing();

        var suppressErrorToasts = configuration.SuppressPenumbraErrorToasts;
        if (ImGui.Checkbox("Hide error pop-ups", ref suppressErrorToasts))
        {
            configuration.SuppressPenumbraErrorToasts = suppressErrorToasts;
            configuration.Save();
        }

        var removeErrorMessages = configuration.RemovePenumbraErrorMessages;
        if (ImGui.Checkbox("Automatically remove matching errors from Penumbra's Messages tab", ref removeErrorMessages))
        {
            configuration.RemovePenumbraErrorMessages = removeErrorMessages;
            configuration.Save();
        }

        var debugMessages = configuration.DebugPenumbraMessages;
        if (ImGui.Checkbox("Debug message removal (writes details to /xllog)", ref debugMessages))
        {
            configuration.DebugPenumbraMessages = debugMessages;
            configuration.Save();
        }

        ImGui.TextWrapped("Applies to these messages: \"Forbidden File Encountered\", \"Forbidden File Redirection\", \"Reserved File Redirection\", and \"Collection without ID found\".");
        ImGui.EndDisabled();

        ImGui.Spacing();
        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Allow moving the settings window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }
    }
}
