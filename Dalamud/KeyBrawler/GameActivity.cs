using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace KeyBrawler;

internal sealed unsafe class GameActivity : IActivityTimers
{
    public bool MinigameActive => Observe().IsActive;

    public MinigamePresence Observe()
    {
        var framework = EventFramework.Instance();
        var director = framework == null ? null : framework->GetInstanceContentDirector();
        var instance = director != null && director->InstanceContentType == InstanceContentType.KeyboundBrawler;
        var module = AgentModule.Instance();
        var agent = module == null ? null : module->GetAgentByInternalId(AgentId.KTGHud);
        var agentActive = agent != null && agent->IsAgentActive();
        var manager = RaptureAtkUnitManager.Instance();
        bool Visible(string name)
        {
            var addon = manager == null ? null : manager->GetAddonByName(name);
            return addon != null && addon->IsReady && addon->IsVisible;
        }
        return new MinigamePresence(instance, agentActive, Visible(MinigameTargetPolicy.AddonName),
            Visible("KTGHudScore") || Visible("KTGHudGaugeHp") || Visible("KTGHudTodo"));
    }

    public void ResetInputTimers()
    {
        // Recheck scope immediately before writing. Never change timer limits or the auto-AFK setting.
        if (!MinigameActive) return;
        var timers = InputTimerModule.Instance();
        if (timers == null) throw new InvalidOperationException("Oyunun giriş zamanlayıcısı bulunamadı; hareketsizlik koruması uygulanamadı.");
        if (!float.IsFinite(timers->ContentInputTimer) || !float.IsFinite(timers->InputTimer))
            throw new InvalidOperationException("Giriş zamanlayıcısı beklenen biçimde değil; işlem durduruldu.");
        timers->ContentInputTimer = 0;
        timers->InputTimer = 0;
    }
}
