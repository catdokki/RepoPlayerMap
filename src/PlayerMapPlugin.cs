using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace RepoPlayerMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class PlayerMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "catdokki.repo.playermap";
        public const string PluginName = "Repo Player Map";
        public const string PluginVersion = "0.2.10";

        // Auto scan config
        private const float AutoScanIntervalSeconds = 5f;
        private const int MaxAutoScans = 30;

        private float _nextScanAt;
        private int _scanCount;
        private bool _didSuccessfulScan;

        private void Awake()
        {
            // Make THIS plugin object persistent
            try
            {
                // Important: call on the GameObject, not just component
                DontDestroyOnLoad(gameObject);

                // These flags often prevent “cleanup” systems from nuking it
                gameObject.hideFlags = HideFlags.HideAndDontSave;

                Logger.LogInfo($"[{PluginName}] Awake: marked persistent. GO={gameObject.name} scene={gameObject.scene.name} flags={gameObject.hideFlags}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{PluginName}] Awake: failed to mark persistent: {ex}");
            }

            // Subscribe to scene events (this also proves we stay alive)
            SceneManager.sceneLoaded += OnSceneLoaded;

            ArmScan("Startup");
        }

        private void OnDestroy()
        {
            // If you see this early, we know the object is still getting killed.
            Logger.LogWarning($"[{PluginName}] OnDestroy fired! GO={gameObject.name} scene={gameObject.scene.name}");
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"[{PluginName}] SceneLoaded: {scene.name} mode={mode}");
            ArmScan($"SceneLoaded:{scene.name}");
        }

        private void ArmScan(string reason)
        {
            _didSuccessfulScan = false;
            _scanCount = 0;
            _nextScanAt = Time.realtimeSinceStartup + 2f; // small delay
            Logger.LogInfo($"[{PluginName}] Scan armed ({reason}). First scan at t={_nextScanAt:0.00}");
        }

        private void Update()
        {
            if (_didSuccessfulScan) return;
            if (_scanCount >= MaxAutoScans) return;

            if (Time.realtimeSinceStartup < _nextScanAt) return;

            _scanCount++;
            _nextScanAt = Time.realtimeSinceStartup + AutoScanIntervalSeconds;

            Logger.LogInfo($"[{PluginName}] AutoScan tick #{_scanCount} (t={Time.realtimeSinceStartup:0.00})");

            try
            {
                int hits = DumpPlayerLikeObjects_ReturnHitCount();
                if (hits > 0)
                {
                    _didSuccessfulScan = true;
                    Logger.LogInfo($"[{PluginName}] AutoScan SUCCESS ({hits} hits). Stopping autoscan.");

                    // Attempt to find local player root after successful scan
                    var root = TryFindLocalPlayerRoot();
                    if (root != null)
                        Logger.LogInfo($"[{PluginName}] LocalPlayerRoot = {root.name} pos={root.position}");
                    else
                        Logger.LogWarning($"[{PluginName}] LocalPlayerRoot NOT FOUND");

                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{PluginName}] AutoScan exception: {ex}");
            }
        }

        private int DumpPlayerLikeObjects_ReturnHitCount()
        {
            Logger.LogInfo("===== RepoPlayerMap: PLAYER OBJECT SCAN START =====");

            int totalHits = 0;

            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            int typeHits = 0;

            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                if (mb.gameObject == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                var tn = mb.GetType().FullName ?? "";
                if (tn.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Photon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var go = mb.gameObject;
                    Logger.LogInfo($"TYPE HIT: {tn} | GO: {go.name} | ACTIVE={go.activeInHierarchy} | POS={go.transform.position}");
                    typeHits++;
                    if (typeHits >= 120) break;
                }
            }

            Logger.LogInfo($"TYPE scan hits: {typeHits}");
            totalHits += typeHits;

            var transforms = Resources.FindObjectsOfTypeAll<Transform>();
            int nameHits = 0;

            foreach (var t in transforms)
            {
                if (t == null) continue;
                var go = t.gameObject;
                if (go == null) continue;
                if (!go.scene.IsValid()) continue;

                var n = go.name ?? "";
                if (n.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("character", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("photon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogInfo($"NAME HIT: GO={go.name} | ACTIVE={go.activeInHierarchy} | POS={t.position}");
                    nameHits++;
                    if (nameHits >= 120) break;
                }
            }

            Logger.LogInfo($"NAME scan hits: {nameHits}");
            totalHits += nameHits;

            Logger.LogInfo("===== RepoPlayerMap: PLAYER OBJECT SCAN END =====");
            return totalHits;
        }

        // Attempt to find the local player's root transform based on known component types
        private Transform TryFindLocalPlayerRoot()
        {
            // Strong candidates based on your scan results:
            // - PlayerAvatar (component)
            // - PlayerHealth (component)
            // - PlayerAccess (component)
            // - PlayerLocalCamera (component)

            // Prefer PlayerAvatar first
            var avatars = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var mb in avatars)
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                var tn = mb.GetType().Name;
                if (tn == "PlayerAvatar")
                    return mb.transform;
            }

            // Fallback: PlayerHealth (often on the avatar controller root)
            foreach (var mb in avatars)
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                var tn = mb.GetType().Name;
                if (tn == "PlayerHealth")
                    return mb.transform;
            }

            // Fallback: Local Camera (less ideal but usable)
            foreach (var mb in avatars)
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                var tn = mb.GetType().Name;
                if (tn == "PlayerLocalCamera")
                    return mb.transform;
            }

            return null;
        }

            }
}
