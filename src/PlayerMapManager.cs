using UnityEngine;
using System;

namespace RepoPlayerMap
{
    public class PlayerMapManager : MonoBehaviour
    {
        private BepInEx.Logging.ManualLogSource _logger;

        private bool _didSuccessfulScan;
        private float _nextAutoScanAt;
        private int _autoScanCount;
        private const float AutoScanIntervalSeconds = 5f;

        public void SetLogger(BepInEx.Logging.ManualLogSource logger) => _logger = logger;

        private void Start()
        {
            _logger?.LogInfo("Repo Player Map: Manager Start()");
            _nextAutoScanAt = Time.realtimeSinceStartup + AutoScanIntervalSeconds;
        }

        private void Update()
        {
            if (!_didSuccessfulScan && Time.realtimeSinceStartup >= _nextAutoScanAt)
            {
                _autoScanCount++;
                _nextAutoScanAt = Time.realtimeSinceStartup + AutoScanIntervalSeconds;

                _logger?.LogInfo($"Repo Player Map: AutoScan tick #{_autoScanCount} (t={Time.realtimeSinceStartup:0.00})");

                try
                {
                    int hits = DumpPlayerLikeObjects_ReturnHitCount();
                    if (hits > 0)
                    {
                        _didSuccessfulScan = true;
                        _logger?.LogInfo($"Repo Player Map: AutoScan SUCCESS ({hits} hits). Stopping autoscan.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Repo Player Map: AutoScan exception: {ex}");
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                _logger?.LogInfo("Repo Player Map: Manual scan triggered (F8)");
                DumpPlayerLikeObjects_ReturnHitCount();
            }
        }

        private int DumpPlayerLikeObjects_ReturnHitCount()
        {
            _logger?.LogInfo("===== RepoPlayerMap: PLAYER OBJECT SCAN START =====");

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
                    tn.IndexOf("Photon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Rig", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("Pawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var go = mb.gameObject;
                    _logger?.LogInfo($"TYPE HIT: {tn} | GO: {go.name} | ACTIVE={go.activeInHierarchy} | POS={go.transform.position}");
                    typeHits++;
                    if (typeHits >= 120) break;
                }
            }

            _logger?.LogInfo($"TYPE scan hits: {typeHits}");
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
                    n.IndexOf("photon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("rig", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("pawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger?.LogInfo($"NAME HIT: GO={go.name} | ACTIVE={go.activeInHierarchy} | POS={t.position}");
                    nameHits++;
                    if (nameHits >= 120) break;
                }
            }

            _logger?.LogInfo($"NAME scan hits: {nameHits}");
            totalHits += nameHits;

            _logger?.LogInfo("===== RepoPlayerMap: PLAYER OBJECT SCAN END =====");
            return totalHits;
        }

        private void OnDestroy()
        {
            _logger?.LogWarning("Repo Player Map: Manager OnDestroy called (should not happen).");
        }
    }
}
