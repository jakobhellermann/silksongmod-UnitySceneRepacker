using System;
using System.IO;
using BepInEx;
using UnitySceneRepacker.Source;
using UnityEngine;

namespace UnitySceneRepacker;

/// Repack assets from another unity game, sharing one cache between all mods.
[BepInAutoPlugin("io.github.jakobhellermann.unityscenerepacker")]
public partial class UnitySceneRepackerPlugin : BaseUnityPlugin {
    public static string CacheDir { get; } = Path.Combine(Paths.CachePath, "unity-scene-repacker");

    /// <see cref="UnitySceneRepacker.RepackedAssets"/> of the game in <paramref name="gameDir"/>. <paramref name="name"/> should be unique to your mod.
    public static RepackedAssets RepackedAssets(string gameDir, string name) => new(gameDir, CacheDir, name);

    /// The installation directory of a game installed next to the running one.
    public static string NextToThisGame(string installDirName) {
        string? installDir = Path.GetDirectoryName(Application.dataPath);
        if (installDir!.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) installDir = Path.GetDirectoryName(installDir);
        return Path.Combine(Path.GetDirectoryName(installDir)!, installDirName);
    }

    private void Awake() {
        Log.Init(Logger);

        var bindings = typeof(GameRepacker).Assembly;
        Log.Info($"Plugin has loaded {bindings.GetName().Name} {bindings.GetName().Version} (format version {GameRepacker.FormatVersion})");
    }
}
