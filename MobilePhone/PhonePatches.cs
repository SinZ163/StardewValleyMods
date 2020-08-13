﻿using Harmony;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Threading.Tasks;
using xTile.Dimensions;

namespace MobilePhone
{
    internal class PhonePatches
    {
        private static IMonitor Monitor;
        private static IModHelper Helper;
        private static ModConfig Config;

        // call this method from your Entry class
        public static void Initialize(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Monitor = monitor;
            Helper = helper;
            Config = config;
        }
        internal static bool Farmer_changeFriendship_prefix(int amount, NPC n)
        {
            if (ModEntry.isReminiscing)
            {
                Monitor.Log($"Reminiscing, will not change friendship with {n.name} by {amount}");
                return false;
            }
            return true;
        }
        internal static bool Event_command_cutscene_prefix(ref Event __instance, GameLocation location, GameTime time, string[] split, ref ICustomEventScript ___currentCustomEventScript)
        {
            if (!ModEntry.isInviting)
                return true;
            string text = split[1];
            if (___currentCustomEventScript != null)
            {
                if (___currentCustomEventScript.update(time, __instance))
                {
                    ___currentCustomEventScript = null;
                    int num = __instance.CurrentCommand;
                    __instance.CurrentCommand = num + 1;
                    return false;
                }
            }

            if (Game1.currentMinigame != null)
                return true;

            if (text == "EventInvite_balloonChangeMap")
            {
                location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(0, 1183, 84, 160), 10000f, 1, 99999, new Vector2(22f, 36f) * 64f + new Vector2(-23f, 0f) * 4f, false, false, 2E-05f, 0f, Color.White, 4f, 0f, 0f, 0f, false)
                {
                    motion = new Vector2(0f, -2f),
                    yStopCoordinate = 576,
                    reachedStopCoordinate = new TemporaryAnimatedSprite.endBehavior(balloonInSky),
                    attachedCharacter = __instance.farmer,
                    id = 1
                });
                location.TemporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(84, 1205, 38, 26), 10000f, 1, 99999, new Vector2(22f, 36f) * 64f + new Vector2(0f, 134f) * 4f, false, false, 0.2625f, 0f, Color.White, 4f, 0f, 0f, 0f, false)
                {
                    motion = new Vector2(0f, -2f),
                    id = 2,
                    attachedCharacter = __instance.getActorByName(ModEntry.invitedNPC.Name)
                });
                int num = __instance.CurrentCommand;
                __instance.CurrentCommand = num + 1;
                Game1.globalFadeToClear(null, 0.01f);
                return false;
            }
            if (text == "EventInvite_balloonDepart")
            {
                TemporaryAnimatedSprite temporarySpriteByID = location.getTemporarySpriteByID(1);
                temporarySpriteByID.attachedCharacter = __instance.farmer;
                temporarySpriteByID.motion = new Vector2(0f, -2f);
                TemporaryAnimatedSprite temporarySpriteByID2 = location.getTemporarySpriteByID(2);
                temporarySpriteByID2.attachedCharacter = __instance.getActorByName(ModEntry.invitedNPC.Name);
                temporarySpriteByID2.motion = new Vector2(0f, -2f);
                location.getTemporarySpriteByID(3).scaleChange = -0.01f;
                int num = __instance.CurrentCommand;
                __instance.CurrentCommand = num + 1;
                return false;
            }
            return true;
        }

        private static void balloonInSky(int extraInfo)
        {
            TemporaryAnimatedSprite t = Game1.currentLocation.getTemporarySpriteByID(2);
            if (t != null)
            {
                t.motion = Vector2.Zero;
            }
            t = Game1.currentLocation.getTemporarySpriteByID(1);
            if (t != null)
            {
                t.motion = Vector2.Zero;
            }
        }

        internal static bool Event_command_prefix(Event __instance, string[] split)
        {
            if (ModEntry.isReminiscing)
            {
                Monitor.Log($"Reminiscing, will not execute event command {string.Join(" ",split)}");
                int num = __instance.CurrentCommand;
                __instance.CurrentCommand = num + 1;
                return false;
            }
            return true;
        }
        internal static bool Event_endBehaviors_prefix(Event __instance, string[] split)
        {
            if (ModEntry.isReminiscing)
            {
                Monitor.Log($"Reminiscing, will not execute end behaviors {string.Join(" ", split)}");
                __instance.exitEvent();
                return false;
            }
            return true;
        }

        internal static bool CarpenterMenu_returnToCarpentryMenu_prefix()
        {
            if (!ModEntry.inCall)
                return true;
            LocationRequest locationRequest = ModEntry.callLocation;

            locationRequest.OnWarp += delegate ()
            {
                RefreshView1();
            };
            Game1.warpFarmer(locationRequest, Game1.player.getTileX(), Game1.player.getTileY(), Game1.player.facingDirection);
            return false;
        }

        internal static bool CarpenterMenu_returnToCarpentryMenuAfterSuccessfulBuild_prefix()
        {
            if (!ModEntry.inCall)
                return true;
            LocationRequest locationRequest = ModEntry.callLocation;
            locationRequest.OnWarp += delegate ()
            {
                RefreshView2();

            };
            Game1.warpFarmer(locationRequest, Game1.player.getTileX(), Game1.player.getTileY(), Game1.player.facingDirection);
            return false;
        }
        private static void RefreshView1()
        {
            if (!(Game1.activeClickableMenu is CarpenterMenu))
                return;

            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "onFarm").SetValue(false);
            Game1.player.viewingLocation.Value = null;
            Helper.Reflection.GetMethod(Game1.activeClickableMenu, "resetBounds").Invoke(new object[] { });
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "upgrading").SetValue(false);
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "moving").SetValue(false);
            Helper.Reflection.GetField<Building>(Game1.activeClickableMenu, "buildingToMove").SetValue(null);
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "freeze").SetValue(false);
            Game1.displayHUD = true;
            Game1.viewportFreeze = false;
            Game1.viewport.Location = ModEntry.callViewportLocation;
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "drawBG").SetValue(true);
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "demolishing").SetValue(false);
            Game1.displayFarmer = true;
            if (Game1.options.SnappyMenus)
            {
                Game1.activeClickableMenu.populateClickableComponentList();
                Game1.activeClickableMenu.snapToDefaultClickableComponent();
            }
        }
        private static void RefreshView2()
        {
            if (!(Game1.activeClickableMenu is CarpenterMenu))
                return;


            Game1.displayHUD = true;
            Game1.player.viewingLocation.Value = null;
            Game1.viewportFreeze = false;
            Game1.viewport.Location = new Location(320, 1536);
            Helper.Reflection.GetField<bool>(Game1.activeClickableMenu, "freeze").SetValue(false);
            Game1.displayFarmer = true;
            robinPhoneConstructionMessage(Game1.activeClickableMenu, (Game1.activeClickableMenu as CarpenterMenu).CurrentBlueprint);
        }

        private static async void robinPhoneConstructionMessage(IClickableMenu instance, BluePrint CurrentBlueprint)
        {
            Game1.player.forceCanMove();
            string dialoguePath = "Data\\ExtraDialogue:Robin_" + (Helper.Reflection.GetField<bool>(instance, "upgrading").GetValue() ? "Upgrade" : "New") + "Construction";
            if (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason))
            {
                dialoguePath += "_Festival";
            }
            if (CurrentBlueprint.daysToConstruct <= 0)
            {
                Game1.drawDialogue(Game1.getCharacterFromName("Robin", true), Game1.content.LoadString("Data\\ExtraDialogue:Robin_Instant", (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.de) ? CurrentBlueprint.displayName : CurrentBlueprint.displayName.ToLower()));
            }
            else
            {
                Game1.drawDialogue(Game1.getCharacterFromName("Robin", true), Game1.content.LoadString(dialoguePath, (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.de) ? CurrentBlueprint.displayName : CurrentBlueprint.displayName.ToLower(), (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.de) ? CurrentBlueprint.displayName.Split(' ').Last().Split('-').Last() : ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.es || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.it) ? CurrentBlueprint.displayName.ToLower().Split(' ').First() : CurrentBlueprint.displayName.ToLower().Split(' ').Last())));
            }

            while (Game1.activeClickableMenu is DialogueBox)
            {
                await Task.Delay(50);
            }
            MobilePhoneCall.ShowMainCallDialogue(ModEntry.callingNPC);
        }
    }
}