using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace RepoPlayerMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class PlayerMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "catdokki.repo.playermap";
        public const string PluginName = "Repo Player Map";
        public const string PluginVersion = "0.1.1";

        // Map panel layout
        private Rect _mapRect = new Rect(20f, 20f, 250f, 250f);

        // Tweak this to match the scale of REPO’s world -> map
        private float _mapScale = 0.05f;

        private GUIStyle _labelStyle;
        private Texture2D _circleTexture;

        private void Awake()
        {
            Logger.LogInfo($"Repo Player Map v{PluginVersion} loaded.");

            // IMPORTANT: Do NOT touch GUI / GUI.skin / GUIStyle here.
            // Only safe init (non-GUI) belongs in Awake.

            // If you want, you can create textures here WITHOUT calling GUI.skin.
            if (_circleTexture == null)
            {
                _circleTexture = new Texture2D(1, 1);
                _circleTexture.SetPixel(0, 0, Color.white);
                _circleTexture.Apply();
            }
        }


        private void OnGUI()
        {
            
            EnsureGuiInitialized();

            // Optional: only show when map is open in REPO
            if (!IsMapOpen())
                return;

            GUI.Box(_mapRect, "Players");

            // Get player info from REPO
            List<PlayerInfo> players = FetchPlayers();
            PlayerInfo? local = GetLocalPlayer(players);
            if (local == null)
                return;

            Vector3 centerPos = local.Value.WorldPosition;

            foreach (var p in players)
            {
                DrawPlayerDot(p, centerPos);
            }
        }
        
        private void EnsureGuiInitialized()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
                _labelStyle.normal.textColor = Color.white;
            }

            if (_circleTexture == null)
            {
                _circleTexture = new Texture2D(1, 1);
                _circleTexture.SetPixel(0, 0, Color.white);
                _circleTexture.Apply();
            }
        }


        private void DrawPlayerDot(PlayerInfo player, Vector3 centerPos)
        {
            // Convert world to "map space" relative to local player
            Vector3 delta = player.WorldPosition - centerPos;
            float x = _mapRect.x + _mapRect.width / 2f + delta.x * _mapScale;
            float y = _mapRect.y + _mapRect.height / 2f - delta.z * _mapScale; 
            // Using z as “forward” in Unity

            // Clamp to panel bounds
            if (x < _mapRect.x) x = _mapRect.x;
            if (x > _mapRect.xMax) x = _mapRect.xMax;
            if (y < _mapRect.y) y = _mapRect.y;
            if (y > _mapRect.yMax) y = _mapRect.yMax;

            const float radius = 10f;
            Rect circleRect = new Rect(
                x - radius,
                y - radius,
                radius * 2f,
                radius * 2f
            );

            // Draw colored circle-ish (square, but we can fake round)
            Color prevColor = GUI.color;
            GUI.color = player.Color;
            GUI.DrawTexture(circleRect, _circleTexture);
            GUI.color = prevColor;

            // Draw initial
            string initial = !string.IsNullOrEmpty(player.Name) ? player.Name.Substring(0, 1).ToUpper() : "?";
            GUI.Label(circleRect, initial, _labelStyle);
        }

        #region REPO-SPECIFIC API (to implement with the actual SDK)

        // Replace this with real player info from the REPO SDK
        private List<PlayerInfo> FetchPlayers()
        {
            var result = new List<PlayerInfo>();

            // TODO: Use REPO's multiplayer/player manager API here.
            //
            // Example pseudo-code if REPO exposes something like:
            // foreach (var p in RepoGame.PlayerManager.AllPlayers)
            // {
            //     result.Add(new PlayerInfo
            //     {
            //         Name = p.DisplayName,
            //         WorldPosition = p.Transform.position,
            //         Color = GetColorForPlayer(p)
            //     });
            // }

            // TEMP: Fake demo data (for testing in a blank scene)
            // Remove this once you wire the real API.
            /*
            result.Add(new PlayerInfo
            {
                Name = "You",
                WorldPosition = Vector3.zero,
                Color = Color.green
            });

            result.Add(new PlayerInfo
            {
                Name = "A",
                WorldPosition = new Vector3(50f, 0f, 20f),
                Color = Color.cyan
            });

            result.Add(new PlayerInfo
            {
                Name = "B",
                WorldPosition = new Vector3(-40f, 0f, -15f),
                Color = Color.magenta
            });
            */

            return result;
        }

        private PlayerInfo? GetLocalPlayer(List<PlayerInfo> players)
        {
            // TODO: Use REPO's concept of "local player".
            // For now, just take the first one.
            if (players.Count == 0)
                return null;

            return players[0];
        }

        private bool IsMapOpen()
        {
            // TODO: Hook into REPO's map UI states.
            // For now, always true.
            return true;
        }

        #endregion
    }

    public struct PlayerInfo
    {
        public string Name;
        public Vector3 WorldPosition;
        public Color Color;
    }
}
