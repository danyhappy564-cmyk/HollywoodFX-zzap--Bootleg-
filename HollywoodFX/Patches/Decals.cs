using System.Reflection;
using SPT.Reflection.Patching;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace HollywoodFX.Patches;

/*
 * This patch is required because BSG now applies switching between player model LOD levels. Unfortunately decals are tied to renderers which are
 * themselves tied to specific LODs. If a decal is placed on a far away LOD level, it will not be visible up close (and vice versa). So we simply
 * apply decals irrespective of visibility. This will also ensure that dead bodies from firefights far away get covered in blood as appropriate.
 */
class TextureDecalsPainterVisCheckPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Was method_5(Renderer) until 4.1 gave it a name.
        return typeof(TextureDecalsPainter).GetMethod(nameof(TextureDecalsPainter.IsCorrectRenderer));
    }

    [PatchPrefix]
    public static bool Prefix(TextureDecalsPainter __instance, Renderer objRenderer, ref bool __result)
    {
        // objRenderer.isVisible nixed
        //
        // Dropping that test also dropped the liveness gate that came with it. BSG calls
        // this from DrawDecal, which walks a List<Renderer> that Effects.PlayerMeshesHit
        // handed it, and on the blood path that list belongs to a body that is being shot
        // at - sometimes the same frame it is being culled or disposed. isVisible was
        // false for those and the original never got as far as the material.
        //
        // 2026-09-15 field log, six times in one raid, at IL offset 0 of this method -
        // the first dereference, i.e. objRenderer itself:
        //
        //   NullReferenceException
        //     HollywoodFX.Patches.TextureDecalsPainterVisCheckPatch.Prefix
        //     TextureDecalsPainter.DrawDecal
        //     Systems.Effects.Effects.PlayerMeshesHit
        //     EFT.Player.ShotReactions
        //
        // The same log has EFT.Player.Dispose and OfflinePlayerCulling.ApplyVisibleState
        // throwing around those timestamps, which is the body this was asked about.
        //
        // == is Unity's overload, so this covers a destroyed renderer as well as a plain
        // null entry in the list.
        if (objRenderer == null)
        {
            __result = false;
            return false;
        }

        // sharedMaterial rather than material. This is a read-only predicate, and
        // Renderer.material instantiates a private copy of the material on every renderer
        // it is asked about, permanently breaking that renderer out of batching just to
        // read a shader name. Where a copy already exists, sharedMaterial returns that
        // same copy, so the shader being tested does not change either way. It is also
        // null on a renderer with no material assigned, which material would have thrown
        // on.
        var material = objRenderer.sharedMaterial;

        __result = objRenderer.enabled && objRenderer.gameObject.activeSelf &&
                   material != null && material.shader != null &&
                   material.shader.name.Contains("_Decal");

        return false;
    }
}