using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using Comfort.Common;

namespace AutoRanging
{
    [BepInPlugin("com.vultify.autoranging", "AutoRanging", "1.0.0")]
    public class AutoRangingPlugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> _modEnabled;
        private ConfigEntry<bool> _autoRangeEnabled;
        private ConfigEntry<KeyboardShortcut> _rangeKey;

        private float _raycastInterval = 0.3f;
        private float _timer;
        private int _lastSetIndex = -1;
        private Camera _mainCamera;

        private void Awake()
        {
            _modEnabled = Config.Bind(
                "1. General",
                "Enable Mod",
                true,
                "Master toggle — enables or disables the entire mod");

            _autoRangeEnabled = Config.Bind(
                "2. Ranging",
                "Auto Range",
                true,
                "Automatically adjust zeroing while ADS. When disabled, use the keybind to manually range.");

            _rangeKey = Config.Bind(
                "2. Ranging",
                "Manual Range Key",
                new KeyboardShortcut(KeyCode.R, KeyCode.LeftAlt),
                "Keybind to manually range your sight (only works when Auto Range is disabled)");
        }

        private void Update()
        {
            if (!_modEnabled.Value)
                return;

            if (_autoRangeEnabled.Value)
            {
                _timer += Time.deltaTime;
                if (_timer >= _raycastInterval)
                {
                    _timer = 0f;
                    try { DoRange(); } catch { }
                }
            }
            else
            {
                if (UnityEngine.Input.GetKeyDown(_rangeKey.Value.MainKey))
                {
                    bool allModifiersHeld = true;
                    foreach (var mod in _rangeKey.Value.Modifiers)
                    {
                        if (!UnityEngine.Input.GetKey(mod))
                        {
                            allModifiersHeld = false;
                            break;
                        }
                    }
                    if (allModifiersHeld)
                    {
                        try { DoRange(); } catch { }
                    }
                }
            }
        }

        private void DoRange()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
                return;

            var player = gameWorld.MainPlayer;
            if (player == null)
                return;

            var pwa = player.ProceduralWeaponAnimation;
            if (pwa == null || !pwa.IsAiming)
            {
                _lastSetIndex = -1;
                return;
            }

            var sight = pwa.CurrentAimingMod;
            if (sight == null)
                return;

            int scopeIndex = sight.SelectedScopeIndex;
            if (!sight.HasOpticCalibrationPoints(scopeIndex))
                return;

            int[] distances = sight.GetScopeCalibrationDistances(scopeIndex);
            if (distances == null || distances.Length <= 1)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera == null)
                return;

            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            float hitDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMaskClass.HighPolyWithTerrainMask))
            {
                hitDistance = hit.distance;
            }
            else
            {
                hitDistance = 1000f;
            }

            int bestIndex = FindClosestZeroIndex(distances, hitDistance);

            if (bestIndex != _lastSetIndex)
            {
                _lastSetIndex = bestIndex;
                sight.SetSelectedOpticCalibrationPoint(scopeIndex, bestIndex);
                pwa.CalculateLocalSightTarget();
            }
        }

        private int FindClosestZeroIndex(int[] distances, float targetDistance)
        {
            int bestIndex = 0;
            float bestDiff = float.MaxValue;

            for (int i = 0; i < distances.Length; i++)
            {
                float diff = Mathf.Abs(distances[i] - targetDistance);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
