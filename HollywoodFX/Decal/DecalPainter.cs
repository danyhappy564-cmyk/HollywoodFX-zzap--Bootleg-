using System.Collections.Generic;
using DeferredDecals;
using EFT.Ballistics;
using HarmonyLib;
using UnityEngine;

namespace HollywoodFX.Decal;

public class DecalPainter
{
    private readonly DeferredDecalRenderer _renderer;

    // 4.1 stopped obfuscating these as dictionary_0/dictionary_2 - they're now
    // named _meshesDict/_cameras directly, so the ObfuscatedField type-matching
    // workaround is no longer needed. Resolved by name via Traverse instead.
    private readonly Dictionary<Material, DeferredDecalRenderer.ManagedMesh> _meshesDict;
    private readonly Dictionary<Camera, DeferredDecalRenderer.CameraData> _cameras;

    public DecalPainter(DeferredDecalRenderer renderer)
    {
        _renderer = renderer;
        var traverse = Traverse.Create(_renderer);
        _meshesDict = traverse.Field("_meshesDict").GetValue<Dictionary<Material, DeferredDecalRenderer.ManagedMesh>>();
        _cameras = traverse.Field("_cameras").GetValue<Dictionary<Camera, DeferredDecalRenderer.CameraData>>();
    }

    public void DrawDecal(
        DeferredDecalRenderer.SingleDecal decal,
        Vector3 position,
        Vector3 normal,
        BallisticCollider hitCollider,
        float projectorHeight=0.1f)
    {
        if (!_meshesDict.ContainsKey(decal.DecalMaterial))
        {
            foreach (var keyValuePair in _cameras)
                keyValuePair.Value.IsStaticBufferDirty = true;
            _renderer.CreateDecalMesh(decal);
        }
        _renderer.AddCubeToMesh(position, normal, _meshesDict[decal.DecalMaterial], decal, projectorHeight);
    }
}