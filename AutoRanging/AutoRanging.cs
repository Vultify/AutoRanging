using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using Comfort.Common;

namespace AutoRanging
{
    [BepInPlugin("com.vultify.autoranging", "AutoRanging", "2.0.0")]
    public class AutoRangingPlugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> _modEnabled;
        private ConfigEntry<bool> _autoRangeEnabled;
        private ConfigEntry<KeyboardShortcut> _rangeKey;

        // static so the calibration patch can read it
        internal static ConfigEntry<bool> AmmoAwareZeroing;

        private float _raycastInterval = 0.3f;
        private float _timer;
        private int _lastSetIndex = -1;
        private Camera _mainCamera;
        private AmmoTemplate _lastAmmo;

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

            AmmoAwareZeroing = Config.Bind(
                "2. Ranging",
                "Ammo Aware Zeroing",
                true,
                "Calibrate zeroing from the round actually loaded instead of the weapon's default ammo, so drop matches the velocity of what you are firing");

            new Harmony("com.vultify.autoranging").PatchAll(Assembly.GetExecutingAssembly());
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

            RefreshCalibrationOnAmmoChange(player, pwa);

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
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayersMaskController.HighPolyWithTerrainMask))
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

        // Calibration points are cached and only rebuilt when the weapon itself changes, so
        // swapping to a different round mid-raid would otherwise keep the old ballistics.
        private void RefreshCalibrationOnAmmoChange(Player player, ProceduralWeaponAnimation pwa)
        {
            if (!AmmoAwareZeroing.Value)
                return;

            var weapon = player.HandsController?.Item as Weapon;
            if (weapon == null)
            {
                _lastAmmo = null;
                return;
            }

            var ammo = weapon.CurrentAmmoTemplate;
            if (ReferenceEquals(ammo, _lastAmmo))
                return;

            _lastAmmo = ammo;
            weapon.RecalculateOpticCalibrationPoints();
            pwa.CalculateLocalSightTarget();
            _lastSetIndex = -1;
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

    // EFT builds a scope's calibration points from Template.DefAmmoTemplate, so a 7.62x51 rifle is
    // always zeroed for M80 regardless of what is chambered — the further the loaded round sits from
    // that default, the more the zero lies. Same trajectory solver, real ammo fed into it.
    [HarmonyPatch(typeof(Weapon), nameof(Weapon.RecalculateScopeCalibrationPoints))]
    internal static class AmmoAwareCalibrationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Weapon __instance, SightComponent sight, int scopeIndex)
        {
            try
            {
                if (AutoRangingPlugin.AmmoAwareZeroing == null || !AutoRangingPlugin.AmmoAwareZeroing.Value)
                    return true;

                var ammo = __instance.CurrentAmmoTemplate;
                if (ammo == null || ReferenceEquals(ammo, __instance.Template?.DefAmmoTemplate))
                    return true;

                int[] distances = sight.GetScopeCalibrationDistances(scopeIndex);
                if (distances == null || sight.OpticCalibrationPoints == null)
                    return true;

                var unique = new List<int>();
                foreach (int d in distances)
                {
                    if (!unique.Contains(d))
                        unique.Add(d);
                }
                unique.Sort();

                var solved = __instance.CreateOpticCalibrationData(
                    unique.ToArray(), ammo, __instance.SpeedFactor, 0.001f);
                if (solved == null)
                    return true;

                __instance._opticCalibrationPoints = solved;

                var mapped = new Vector3[distances.Length];
                for (int i = 0; i < distances.Length; i++)
                {
                    mapped[i] = solved[unique.IndexOf(distances[i])];
                }

                sight.OpticCalibrationPoints[scopeIndex] = mapped;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
