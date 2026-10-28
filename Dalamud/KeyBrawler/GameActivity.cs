using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace KeyBrawler;

internal sealed unsafe class GameActivity : IActivityTimers
{
    public bool MinigameActive
    {
        get
        {
            var framework = EventFramework.Instance();
            var director = framework == null ? null : framework->GetInstanceContentDirector();
            if (director == null || director->InstanceContentType != InstanceContentType.KeyboundBrawler) return false;
            var module = AgentModule.Instance();
            var agent = module == null ? null : module->GetAgentByInternalId(AgentId.KTGHud);
            return agent != null && agent->IsAgentActive();
        }
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
