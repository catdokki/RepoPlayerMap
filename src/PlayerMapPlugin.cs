using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI; 
using System.Linq;


namespace RepoPlayerMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class PlayerMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "catdokki.repo.playermap";
        public const string PluginName = "Repo Player Map";
        public const string PluginVersion = "0.3.2";

        // Auto scan config
        private const float AutoScanIntervalSeconds = 5f;
        private const int MaxAutoScans = 30;

        private float _nextScanAt;
        private int _scanCount;
        private bool _didSuccessfulScan;


        private Transform _localPlayerRoot;


        private static Sprite _solidSprite;

        private readonly System.Collections.Generic.Dictionary<int, GameObject> _markersByGraphic = new System.Collections.Generic.Dictionary<int, GameObject>();



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
            _nextScanAt = Time.realtimeSinceStartup + 2f;

            // ✅ reset cached references on scene changes
            _localPlayerRoot = null;
            _mapPlayerGraphicTf = null;

            // ✅ kill ALL old markers
            foreach (var kv in _markersByGraphic.Values)
            {
                if (kv != null) Destroy(kv);
            }
            _markersByGraphic.Clear();


            Logger.LogInfo($"[{PluginName}] Scan armed ({reason}). First scan at t={_nextScanAt:0.00}");
        }


        private void Update()
        {
            // Debug hotkey can always work
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Logger.LogInfo($"[{PluginName}] F8 pressed -> dumping all PlayerAvatars");
                DumpAllPlayerAvatarControllers();

            }

            // ---- AUTOSCAN PHASE (runs until success or max scans) ----
            if (!_didSuccessfulScan && _scanCount < MaxAutoScans && Time.realtimeSinceStartup >= _nextScanAt)
            {
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

                        _localPlayerRoot = TryFindLocalPlayerRoot();
                        if (_localPlayerRoot != null)
                            Logger.LogInfo($"[{PluginName}] LocalPlayerRoot = {_localPlayerRoot.name} pos={_localPlayerRoot.position}");
                        else
                            Logger.LogWarning($"[{PluginName}] LocalPlayerRoot NOT FOUND");

                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[{PluginName}] AutoScan exception: {ex}");
                }
            }

            if (_markersByGraphic.Count == 0)
                EnsureMarkersForAllMapGraphics();





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
                    tn.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0)
                    // tn.IndexOf("Network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    // tn.IndexOf("Photon", StringComparison.OrdinalIgnoreCase) >= 0)
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
                    n.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0 
                    // n.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    // n.IndexOf("photon", StringComparison.OrdinalIgnoreCase) >= 0
                    )
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
            var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            // 1️⃣ Best candidate: PlayerAvatar
            foreach (var mb in all)
            {
                if (mb == null) continue;
                var go = mb.gameObject;
                if (go == null) continue;
                if (!go.scene.IsValid()) continue;

                if (mb.GetType().Name == "PlayerAvatar")
                {
                    var root = ResolvePlayerRoot(mb.transform);
                    if (!IsJunkAvatar(root))
                        return root;
                }
            }

            // 2️⃣ Fallback: PlayerHealth
            foreach (var mb in all)
            {
                if (mb == null) continue;
                var go = mb.gameObject;
                if (go == null) continue;
                if (!go.scene.IsValid()) continue;

                if (mb.GetType().Name == "PlayerHealth")
                {
                    var root = ResolvePlayerRoot(mb.transform);
                    if (!IsJunkAvatar(root))
                        return root;
                }
            }

            // 3️⃣ Fallback: PlayerLocalCamera
            foreach (var mb in all)
            {
                if (mb == null) continue;
                var go = mb.gameObject;
                if (go == null) continue;
                if (!go.scene.IsValid()) continue;

                if (mb.GetType().Name == "PlayerLocalCamera")
                {
                    var root = ResolvePlayerRoot(mb.transform);
                    if (!IsJunkAvatar(root))
                        return root;
                }
            }

            return null;
        }


        private Transform ResolvePlayerRoot(Transform t)
        {
            if (t == null) return null;

            // Walk up until we hit the known root name
            var cur = t;
            while (cur != null)
            {
                if (cur.name == "Player Avatar Controller")
                    return cur;

                cur = cur.parent;
            }

            // Fallback: return original transform
            return t;
        }



        private void DumpAllPlayerAvatarControllers()
        {
            int count = 0;
            foreach (var mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                if (mb.GetType().Name == "PlayerAvatar")
                {
                    var go = mb.gameObject;
                    Logger.LogInfo($"[{PluginName}] PlayerAvatar #{++count}: GO={go.name} pos={go.transform.position} scene={go.scene.name}");
                }
            }

            Logger.LogInfo($"[{PluginName}] PlayerAvatar total found: {count}");
        }


        private bool IsJunkAvatar(Transform root)
        {
            if (root == null) return true;

            var go = root.gameObject;

            // Inactive = junk
            if (!go.activeInHierarchy)
                return true;

            var pos = root.position;

            // Menu / preview / pooled avatars
            if (Mathf.Abs(pos.z) > 500f)
                return true;

            // UI or uninitialized placeholders
            if (pos == Vector3.zero)
                return true;

            // Scene sanity check
            if (!go.scene.IsValid())
                return true;

            return false;
        }


        private Transform FindMapLayerTransform()
        {
            foreach (var mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (!mb.gameObject.scene.IsValid()) continue;

                if (mb.GetType().Name == "MapLayer")
                {
                    Logger.LogInfo($"[Repo Player Map] Found MapLayer on {mb.gameObject.name}");
                    return mb.transform;
                }
            }

            Logger.LogWarning("[Repo Player Map] MapLayer NOT found.");
            return null;
        }


        private static Sprite GetSolidSprite()
        {
            if (_solidSprite != null) return _solidSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            _solidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return _solidSprite;
        }


        private void DebugWhichCamerasCanSeeMarker(GameObject marker)
        {
            if (marker == null) return;

            var cams = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (var cam in cams)
            {
                if (cam == null) continue;
                if (!cam.gameObject.scene.IsValid()) continue;

                bool canSeeLayer = (cam.cullingMask & (1 << marker.layer)) != 0;
                if (canSeeLayer)
                {
                    Logger.LogInfo($"[{PluginName}] Camera '{cam.name}' CAN see marker layer {marker.layer} | pos={cam.transform.position} enabled={cam.enabled} active={cam.gameObject.activeInHierarchy}");
                }
            }
        }


        private string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        // We will attempt to find every player graphic 
        private Transform[] FindAllMapPlayerGraphics()
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .Where(t => t != null
                    && t.gameObject.scene.IsValid()
                    && t.name == "Player Graphic"
                    && GetPath(t).IndexOf("Map/Active/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
        }

        // Dump logger for all map players.
        private void DumpAllMapPlayerGraphics()
        {
            var found = FindAllMapPlayerGraphics();
            Logger.LogInfo($"[{PluginName}] Map Player Graphic count={found.Length}");
            foreach (var t in found)
                Logger.LogInfo($"[{PluginName}] MapGraphic: active={t.gameObject.activeInHierarchy} path='{GetPath(t)}' layer={t.gameObject.layer}");
        }


        private void EnsureMarkersForAllMapGraphics()
        {
            var graphics = FindAllMapPlayerGraphics();
            if (graphics.Length == 0)
            {
                Logger.LogWarning($"[{PluginName}] No map Player Graphics found yet.");
                return;
            }

            foreach (var g in graphics)
            {
                int key = g.GetInstanceID();
                if (_markersByGraphic.ContainsKey(key)) continue;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                marker.name = $"RepoPlayerMarker_{key}";
                marker.transform.SetParent(g, false);

                marker.transform.localPosition = new Vector3(0f, 0f, -0.2f);
                marker.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

                // Color
                var r = marker.GetComponent<Renderer>();
                if (r != null)
                {
                    var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        r.material = new Material(shader);
                        r.material.color = Color.red;
                    }
                    r.enabled = true;
                }

                marker.layer = g.gameObject.layer;
                marker.SetActive(true);

                _markersByGraphic[key] = marker;

                Logger.LogInfo($"[{PluginName}] Added marker for map graphic path='{GetPath(g)}'");
            }
        }





    }
}
