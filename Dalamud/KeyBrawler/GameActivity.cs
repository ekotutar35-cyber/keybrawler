using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace KeyBrawler;

internal sealed unsafe class GameActivity : IActivityTimers
{
    public bool MinigameActive
    {
        get
        {
            var ui = ReadUi();
            if (ui.Input || ui.Hud) return true;
            // Optional director/agent metadata must never suppress an already visible native minigame UI.
            return InstanceActive();
        }
    }

    private static (bool Input, bool Hud) ReadUi()
    {
        var manager = RaptureAtkUnitManager.Instance();
        bool Visible(string name)
        {
            var addon = manager == null ? null : manager->GetAddonByName(name);
            return addon != null && addon->IsReady && addon->IsVisible;
        }
        return (Visible(MinigameTargetPolicy.AddonName),
            Visible("KTGHudScore") || Visible("KTGHudGaugeHp") || Visible("KTGHudTodo"));
    }

    private static bool InstanceActive()
    {
        try
        {
            var framework = EventFramework.Instance();
            var director = framework == null ? null : framework->GetInstanceContentDirector();
            return director != null && director->InstanceContentType == InstanceContentType.KeyboundBrawler;
        }
        catch { return false; }
    }

    public MinigamePresence Observe()
    {
        var ui = ReadUi();
        var agentActive = false;
        try
        {
            var module = AgentModule.Instance();
            var agent = module == null ? null : module->GetAgentByInternalId(AgentId.KTGHud);
            agentActive = agent != null && agent->IsAgentActive();
        }
        catch { /* Diagnostic metadata is optional. */ }
        return new MinigamePresence(InstanceActive(), agentActive, ui.Input, ui.Hud);
    }

    public string ReadTimerStatus()
    {
        if (!MinigameActive) return UiText.T("Local timers: waiting for the minigame");
        var timers = InputTimerModule.Instance();
        if (timers == null) return UiText.T("Local timers unavailable");
        return UiText.T("Local timers (s): content={0:F1}, input={1:F1}, AFK={2:F1}" , timers->ContentInputTimer, timers->InputTimer, timers->AfkTimer);
    }

    public bool ResetInputTimers()
    {
        // Recheck scope immediately before writing. Never change timer limits or the auto-AFK setting.
        if (!MinigameActive) return false;
        var timers = InputTimerModule.Instance();
        if (timers == null) throw new InvalidOperationException(UiText.T("Game input timers unavailable; idle protection could not be applied."));
        if (!float.IsFinite(timers->ContentInputTimer) || !float.IsFinite(timers->InputTimer) || !float.IsFinite(timers->AfkTimer))
            throw new InvalidOperationException(UiText.T("Unexpected input timer values; update cancelled."));
        timers->ContentInputTimer = 0;
        timers->InputTimer = 0;
        // Negative AFK values represent an existing away state; do not overwrite that state.
        if (timers->AfkTimer >= 0) timers->AfkTimer = 0;
        return timers->ContentInputTimer == 0 && timers->InputTimer == 0 && timers->AfkTimer <= 0;
    }
}
