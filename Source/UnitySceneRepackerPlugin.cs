using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnitySceneRepacker.Source;
using UnityEngine;

namespace UnitySceneRepacker;

/// Repack assets from another unity game, sharing one cache between all mods.
[BepInAutoPlugin("io.github.jakobhellermann.unityscenerepacker")]
public partial class UnitySceneRepackerPlugin : BaseUnityPlugin {
    public static string CacheDir { get; } = Path.Combine(Paths.CachePath, "unity-scene-repacker");

    // HK's Trigger2dEvent adds `PlayMakerUnity2DProxy` in OnEnter, whereas in Silksong it's added during build time when preprocessing `PlayMakerTriggerEnter2D`.
    // By marking the FSMs from HK as not preprocessed, the preprocessing happens on init, fixing the mismatch.
    private static readonly IReadOnlyList<RewriteRule> rewriteRules = [
        new("PlayMaker", "PlayMakerFSM", "fsm.preprocessed", false),
        new("PlayMaker", "PlayMakerFSM", "eventHandlerComponentsAdded", false),
    ];

    /// <see cref="UnitySceneRepacker.RepackedAssets"/> of the game in <paramref name="gameDir"/>. <paramref name="name"/> should be unique to your mod.
    public static RepackedAssets RepackedAssets(string gameDir, string name) => new(gameDir, GameCacheDir(gameDir), name, rewriteRules);

    private static string GameCacheDir(string gameDir) {
        gameDir = Path.GetFullPath(gameDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(CacheDir, $"{Path.GetFileName(gameDir)}-{StableHash(gameDir):x8}");
    }

    // FNV-1a, as string.GetHashCode can change between runs
    private static uint StableHash(string s) {
        uint hash = 2166136261;
        foreach (char c in s) hash = (hash ^ c) * 16777619;
        return hash;
    }

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
