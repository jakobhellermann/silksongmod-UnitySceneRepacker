# Unity Scene Repacker

Bindings for [unity-scene-repacker](https://github.com/jakobhellermann/unity-scene-repacker), allowing you to repack and instantiate objects from other unity games.

## Setup

You can reference the package without shipping the assemblies, since they come distributed as the dependency:

```xml
<PackageReference Include="Silksong.UnitySceneRepacker" Version="0.1.0" ExcludeAssets="runtime"/>
```

```toml
[package.dependencies]
jakobhellermann-UnitySceneRepacker = "0.1.0"
```

## Example usage:

```cs
using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnitySceneRepacker;

namespace ExampleMod.Source;

[BepInAutoPlugin("io.github.yourgithubusername.examplemod")]
[BepInDependency(UnitySceneRepackerPlugin.Id)]
public partial class ExampleModPlugin : BaseUnityPlugin {
    private ConfigEntry<KeyboardShortcut> spawnShortcut = null!;

    private RepackedAssets? hollowKnightAssets;
    private RepackedAsset? tiktikAsset;

    private AsyncOperationHandle<GameObject> tiktikPrefab;

    private void Awake() {
        try {
            spawnShortcut = Config.Bind("Shortcuts", "Spawn Tiktik", new KeyboardShortcut(KeyCode.F2));

            hollowKnightAssets = UnitySceneRepackerPlugin.RepackedAssets(
                UnitySceneRepackerPlugin.NextToThisGame("Hollow Knight"),
                "examplemod"
            );
            tiktikAsset = hollowKnightAssets.Request("Tutorial_01", "_Enemies/Crawler 2");
        } catch (Exception e) {
            Logger.LogInfo($"Plugin {Name} ({Id}) failed to initialize: {e}");
        }
    }

    private void Start() {
        // optional: start repacking requests. Alternative, the first load will perform and wait for the repack.
        hollowKnightAssets?.Prepare();
    }

    private void Update() {
        if (spawnShortcut.Value.IsDown()) StartCoroutine(SpawnTiktik());
    }

    private IEnumerator SpawnTiktik() {
        if (!HeroController.instance) yield break;
        
        Log.Info("Spawning tiktik");

        if (!tiktikPrefab.IsValid()) tiktikPrefab = tiktikAsset!.Load();
        yield return tiktikPrefab;

        Instantiate(tiktikPrefab.Result, HeroController.instance.transform.position, Quaternion.identity);
    }

    private void OnDestroy() {
        hollowKnightAssets?.Dispose();

        Log.Info($"Plugin {Name} ({Id}) has been unloaded!");
    }
}
```
