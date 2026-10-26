using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TaktikmagierVPEPatch
{
    // Taktikmagier and VPE both prefix Hediff_Psylink.TryGiveAbilityOfLevel (vanilla's free random psycast on
    // psylink gain). VPE's always returns false to turn the freebie off. Taktikmagier's re-implements the freebie
    // so its own spells stay out of the random pool, then returns false.
    //
    // Harmony 3 skips a bool-returning prefix once an earlier prefix has returned false, so whichever runs first
    // wins. Both use default priority, so the one registered first runs first: Taktikmagier patches in its Mod
    // constructor, VPE in a StaticConstructorOnStartup, so Taktikmagier's grants the psycast and VPE's is skipped.
    //
    // This re-registers Taktikmagier's own prefix, unchanged, at the lowest priority so it runs after VPE's and is
    // skipped whenever VPE has blocked the original. Taktikmagier's spells don't come from this method (manuals and
    // pawn generation give them directly), so nothing of theirs is lost.
    [StaticConstructorOnStartup]
    public static class ReorderPsylinkGrantPrefix
    {
        private const string HarmonyId = "DanZinagri.Taktikmagier.VPEPatch";
        private const string TaktikmagierHarmonyId = "Aoba.DeadManSwitch.Taktikmagier";
        private const string VPEHarmonyId = "OskarPotocki.VanillaPsycastsExpanded";
        private const string TaktikmagierPrefix = "Militarmagier.Patch_Hediff_Psylink_TryGiveAbilityOfLevel:Prefix";

        static ReorderPsylinkGrantPrefix()
        {
            MethodInfo original = AccessTools.Method(typeof(Hediff_Psylink), "TryGiveAbilityOfLevel");
            MethodInfo prefix = AccessTools.Method(TaktikmagierPrefix);
            Patches patches = original == null ? null : Harmony.GetPatchInfo(original);
            Patch existing = patches?.Prefixes.FirstOrDefault(p => p.PatchMethod == prefix && p.owner == TaktikmagierHarmonyId);
            if (prefix == null || existing == null)
            {
                Log.Warning("[Taktikmagier - VPE Patch] Taktikmagier's psylink prefix not found; leaving prefix order as is.");
                return;
            }

            new Harmony(TaktikmagierHarmonyId).Unpatch(original, prefix);
            new Harmony(HarmonyId).Patch(original, prefix: new HarmonyMethod(prefix)
            {
                priority = Priority.Last,
                after = new[] { VPEHarmonyId }
            });
        }
    }
}
