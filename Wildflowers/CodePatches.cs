﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = StardewValley.Object;

namespace Wildflowers
{
    public partial class ModEntry
    {
        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.DayUpdate))]
        public class GameLocation_DayUpdate_Patch
        {
            public static void Postfix(GameLocation __instance)
            {
                if (!Config.ModEnabled)
                    return;
                cropDict[__instance.Name] = new Dictionary<Vector2, Crop>();
                foreach (var key in __instance.terrainFeatures.Pairs.Where(p => p.Value is Grass).Select(p => p.Key))
                {
                    Crop crop;
                    if (!__instance.terrainFeatures[key].modData.TryGetValue(wildKey, out string data))
                    {
                        var chance = Game1.random.NextDouble();
                        
                        if (chance <= Config.wildflowerGrowChance)
                        {
                            var flowers = Game1.objectInformation.Where(p => p.Value.Contains("/Basic -80/") && !Config.DisallowNames.Contains(p.Value.Split('/')[0])).Select(p => p.Key).ToArray();
                            int idx = GetRandomFlowerSeed(flowers);
                            if (idx < 0)
                            {
                                SMonitor.Log($"no flowers for this season");
                                return;
                            }
                            crop = new Crop(idx, (int)key.X, (int)key.Y);
                            crop.growCompletely();
                            SMonitor.Log($"Added new wild flower {crop.indexOfHarvest} to {__instance.Name} at {key}");
                        }
                        else
                            continue;
                    }
                    else
                    {
                        crop = JsonConvert.DeserializeObject<CropData>(data).ToCrop();
                    }
                    crop.newDay(1, 0, (int)key.X, (int)key.Y, __instance);
                    cropDict[__instance.Name][key] = crop;
                }
            }
        }
        [HarmonyPatch(typeof(Grass), nameof(Grass.draw))]
        public class Grass_draw_Patch
        {
            public static void Postfix(Grass __instance, SpriteBatch spriteBatch, Vector2 tileLocation)
            {
                if (!Config.ModEnabled || !__instance.modData.TryGetValue(wildKey, out string data))
                    return;
                if (!cropDict.TryGetValue(__instance.currentLocation.Name, out Dictionary<Vector2, Crop> locDict) || !locDict.TryGetValue(tileLocation, out Crop crop))
                {
                    if (locDict is null)
                    {
                        cropDict[__instance.currentLocation.Name] = new Dictionary<Vector2, Crop>();
                    }
                    crop = JsonConvert.DeserializeObject<CropData>(data).ToCrop();
                    cropDict[__instance.currentLocation.Name][tileLocation] = crop;
                }
                //locDict.Remove(tileLocation);
                //__instance.modData.Remove(wildKey);
                crop.draw(spriteBatch, tileLocation, Color.White, 0);
            }
        }
        [HarmonyPatch(typeof(Utility), nameof(Utility.findCloseFlower), new Type[] { typeof(GameLocation), typeof(Vector2), typeof(int), typeof(Func<Crop, bool>)})]
        public class Utility_findCloseFlower_Patch
        {
            public static void Postfix(GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool> additional_check, ref Crop __result)
            {
                if (!Config.ModEnabled || !Config.WildFlowersMakeFlowerHoney || !cropDict.TryGetValue(location.Name, out Dictionary<Vector2, Crop> locDict))
                    return;
                Vector2 tilePos = __result is null ? Vector2.Zero : AccessTools.FieldRefAccess<Crop, Vector2>(__result, "tilePosition");
                foreach(var v in locDict.Keys)
                {
                    if (__result is null) {
                        if (range < 0 || Math.Abs(v.X - startTileLocation.X) + Math.Abs(v.Y - startTileLocation.Y) <= range)
                        {
                            __result = locDict[v];
                        }
                    }
                    else if(Vector2.Distance(startTileLocation, v) < Vector2.Distance(tilePos, startTileLocation))
                    {
                        __result = locDict[v];
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Utility), nameof(Utility.canGrabSomethingFromHere))]
        public class Utility_canGrabSomethingFromHere_Patch
        {
            public static void Postfix(int x, int y, Farmer who, ref bool __result)
            {
                if (!Config.ModEnabled || __result || Game1.currentLocation is null || !cropDict.TryGetValue(Game1.currentLocation.Name, out Dictionary<Vector2, Crop> locDict) || !locDict.TryGetValue(new Vector2(x / 64, y / 64), out Crop crop))
                    return;
                __result = crop != null && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0) && crop.currentPhase.Value >= crop.phaseDays.Count - 1 && !crop.dead.Value && (!crop.forageCrop.Value || crop.whichForageCrop.Value != 2);
                if (__result)
                {
                    Game1.mouseCursor = 6;
                    if (!Utility.withinRadiusOfPlayer(x, y, 1, who))
                    {
                        Game1.mouseCursorTransparency = 0.5f;
                        __result = false;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(TerrainFeature), nameof(TerrainFeature.performUseAction))]
        public class TerrainFeature_performUseAction_Patch
        {
            public static void Postfix(TerrainFeature __instance, Vector2 tileLocation, GameLocation location, ref bool __result)
            {
                if (!Config.ModEnabled || __result || __instance is not Grass || !__instance.modData.ContainsKey(wildKey) || location is null || !cropDict.TryGetValue(location.Name, out Dictionary<Vector2, Crop> locDict) || !locDict.TryGetValue(tileLocation, out Crop crop))
                    return;
                if (crop.currentPhase.Value >= crop.phaseDays.Count - 1 && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0))
                {
                    __result = crop.harvest((int)tileLocation.X, (int)tileLocation.Y, new HoeDirt(1, crop));
                    if (__result)
                    {
                        cropDict[location.Name].Remove(tileLocation);
                        __instance.modData.Remove(wildKey);
                        SMonitor.Log($"harvested wild crop in {location.Name} at {tileLocation}");
                    }
                }
            }
        }
    }
}