using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly IReadOnlyList<RewriteRule> playMakerRules = [
        RewriteRule.SetField("PlayMaker", "PlayMakerFSM", "fsm.preprocessed", false),
        RewriteRule.SetField("PlayMaker", "PlayMakerFSM", "eventHandlerComponentsAdded", false),
    ];

    private static readonly SortedDictionary<(string assembly, string script), (string assembly, string script)> remaps = new();
    private static IReadOnlyList<RewriteRule>? rewriteRules;

    /// <see cref="UnitySceneRepacker.RepackedAssets"/> of the game in <paramref name="gameDir"/>. <paramref name="name"/> should be unique to your mod.
    public static RepackedAssets RepackedAssets(string gameDir, string name) => new(gameDir, GameCacheDir(gameDir), name, RewriteRules);

    /// <summary>
    /// Bind all MonoBehaviours of a script in the repacked assets to <paramref name="toScript"/> in <paramref name="toAssembly"/> instead.
    /// If the class is compatible, fields will be initialized like in the original script.
    /// </summary>
    /// Throws once the first <see cref="UnitySceneRepacker.RepackedAssets"/> was prepared or loaded from, or if the script was already remapped.
    public static void RemapScript(string assembly, string script, string toAssembly, string toScript) {
        if (remaps.TryGetValue((assembly, script), out var existing)) {
            if (existing != (toAssembly, toScript))
                throw new InvalidOperationException($"{assembly}:{script} is already remapped to {existing.assembly}:{existing.script}, not {toAssembly}:{toScript}");
            return;
        }
        if (rewriteRules != null)
            throw new InvalidOperationException($"can't remap {assembly}:{script} after assets were prepared or loaded, remap in Awake instead");

        remaps[(assembly, script)] = (toAssembly, toScript);
    }

    /// <inheritdoc cref="RemapScript(string, string, string, string)"/>
    public static void RemapScript(string assembly, string script, Type to) {
        if (!typeof(MonoBehaviour).IsAssignableFrom(to))
            throw new ArgumentException($"{to} is not a MonoBehaviour", nameof(to));
        // unity binds scripts by namespace and class name, which can't express nesting
        if (to.IsNested)
            throw new ArgumentException($"{to} is nested in {to.DeclaringType}", nameof(to));

        RemapScript(assembly, script, to.Assembly.GetName().Name, to.FullName!);
    }

    private static IReadOnlyList<RewriteRule> RewriteRules() =>
        rewriteRules ??= [
            ..playMakerRules,
            ..remaps.Select(remap => RewriteRule.RemapScript(remap.Key.assembly, remap.Key.script, remap.Value.assembly, remap.Value.script)),
        ];

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
