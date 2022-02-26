﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BetterJunimos;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.HomeRenovations;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace BetterJunimosForestry {
    public static class Modes {
        public static readonly string Normal = "normal";
        public static readonly string Crops = "crops";
        public static readonly string Orchard = "orchard";
        public static readonly string Forest = "forest";
        public static readonly string Grains = "grains";
        public static readonly string Maze = "maze";
    }
    
    public class HutState {
        public bool ShowHUD = false;
        public string Mode = Modes.Normal;
    }

    public class ModeChange {
        public Guid guid;
        public string mode;

        public ModeChange(Guid guid, string mode) {
            this.guid = guid;
            this.mode = mode;
        }
    }
    
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod {
        private readonly Dictionary<Rectangle, ModeChange> Rectangles = new();
        
        private readonly Rectangle TreeIcon = new(0, 656, 14, 14);
        private readonly Rectangle JunimoIcon = new(109, 492, 14, 14);
        private readonly Rectangle CropIcon = new(178, 129, 14, 14);
        private readonly Rectangle FruitTreeIcon = new(16, 624, 14, 14);
        private readonly Rectangle ScrollIcon = new(673, 81, 14, 14);
        private readonly Rectangle BundleIcon = new(331, 374, 15, 14);
        private readonly Rectangle LetterIcon = new(190, 422, 14, 14);
        private readonly Rectangle QuestionIcon = new(174, 424, 14, 14);
        private readonly Rectangle MapIcon = new(426, 492, 14, 14);
        private readonly Rectangle CheckboxesIcon = new(225, 424, 22, 14);
        
        internal static ModConfig Config;
        internal static IMonitor SMonitor;
        internal static Dictionary<Vector2, HutState> HutStates;
        internal static Dictionary<Vector2, Maze> HutMazes;

        internal static IBetterJunimosApi BJApi;
        private GenericModConfigMenuAPI GMCMAPI;

        internal static Abilities.PlantTreesAbility PlantTrees;
        internal static Abilities.PlantFruitTreesAbility PlantFruitTrees;

        private void RenderedWorld(object sender, RenderedWorldEventArgs e) {
            if (Game1.player.currentLocation is not Farm) return;
            Rectangles.Clear();
            
            foreach (var (hutPos, hutState) in HutStates)
            {
                RenderHutMenu(e, hutState, hutPos);
            }
        }

        private void RenderHutMenu(RenderedWorldEventArgs e, HutState hutState, Vector2 hutPos)
        {
            if (!hutState.ShowHUD) return;
            var hut = Util.GetHutFromPosition(hutPos);
            var guid = Util.GetHutIdFromHut(hut);
            if (hut == null)
            {
                return;
            }

            const int padding = 3;
            const int offset = 14 * Game1.pixelZoom;

            const int scrollWidth = offset * 7 + padding * 2;
            var hutXvp = hut.tileX.Value * Game1.tileSize - Game1.viewport.X + 1; // hut x co-ord in viewport pixels
            var scrollXvp = (int) (hutXvp + Game1.tileSize * 1.5 - scrollWidth / 2);

            Vector2 origin = new Vector2(scrollXvp,
                (int) hut.tileY.Value * Game1.tileSize - Game1.viewport.Y + 1 + Game1.tileSize * 2 + 16);

            int n = 0;
            Rectangle normal = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle crops = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle orchard = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle forest = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle maze = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle quests = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);
            Rectangle actions = new Rectangle((int) origin.X + padding + offset * n++, (int) origin.Y - 4, 14 * Game1.pixelZoom,
                14 * Game1.pixelZoom);

            var scroll = new Rectangle((int) origin.X, (int) origin.Y, scrollWidth, 18);

            Rectangles[scroll] = new ModeChange(guid, "_menu");
            Rectangles[normal] = new ModeChange(guid, "normal");
            Rectangles[crops] = new ModeChange(guid, "crops");
            Rectangles[orchard] = new ModeChange(guid, "orchard");
            Rectangles[forest] = new ModeChange(guid, "forest");
            Rectangles[maze] = new ModeChange(guid, "maze");
            Rectangles[quests] = new ModeChange(guid, "_quests");
            Rectangles[actions] = new ModeChange(guid, "_actions");

            Util.DrawScroll(e.SpriteBatch, origin, scrollWidth);
            e.SpriteBatch.Draw(Game1.mouseCursors, normal, JunimoIcon,
                Color.White * (hutState.Mode == "normal" ? 1.0f : 0.25f));
            e.SpriteBatch.Draw(Game1.mouseCursors, crops, CropIcon, Color.White * (hutState.Mode == "crops" ? 1.0f : 0.25f));
            e.SpriteBatch.Draw(Game1.mouseCursors, orchard, FruitTreeIcon,
                Color.White * (hutState.Mode == "orchard" ? 1.0f : 0.25f));
            e.SpriteBatch.Draw(Game1.mouseCursors, forest, TreeIcon, Color.White * (hutState.Mode == "forest" ? 1.0f : 0.25f));
            e.SpriteBatch.Draw(Game1.mouseCursors, maze, MapIcon, Color.White * (hutState.Mode == "maze" ? 1.0f : 0.25f));
            e.SpriteBatch.Draw(Game1.mouseCursors, quests, LetterIcon, Color.White);
            e.SpriteBatch.Draw(Game1.mouseCursors, actions, CheckboxesIcon, Color.White);
        }

        void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!Context.IsWorldReady) { return; }
            
            if (e.Button == SButton.MouseLeft) {
                if (Game1.player.currentLocation is not Farm) return;
                if (Game1.activeClickableMenu != null) return;
                
                var hut = Util.HutOnTile(e.Cursor.Tile);
                if (hut is not null) {
                    var hutPos = Util.GetHutPositionFromHut(hut);
                    if (!HutStates.ContainsKey(hutPos)) HutStates[hutPos] = new HutState();
                    HutStates[hutPos].ShowHUD = !HutStates[hutPos].ShowHUD;
                    Helper.Input.Suppress(SButton.MouseLeft);
                    return;
                }
                
                HandleMenuClick(e);
            }
        }

        private void HandleMenuClick(ButtonPressedEventArgs e)
        {
            foreach (var (r, mc) in Rectangles)
            {
                bool contains = r.Contains((int) e.Cursor.ScreenPixels.X, (int) e.Cursor.ScreenPixels.Y);
                if (contains)
                {
                    Helper.Input.Suppress(SButton.MouseLeft);
                    var hut = Util.GetHutFromId(mc.guid);
                    Vector2 hutPos = Util.GetHutPositionFromId(mc.guid);
                    // Monitor.Log($"Rectangle {r} {mc.mode} {r.X} {r.Y} contains: {contains}");
                    if (mc.mode == "_quests")
                    {
                        BJApi.ShowPerfectionTracker();
                    }

                    if (mc.mode == "_actions")
                    {
                        BJApi.ShowConfigurationMenu();
                        BJApi.ListAvailableActions(mc.guid);
                    }

                    if (!mc.mode.StartsWith("_"))
                    {
                        HutStates[hutPos].Mode = mc.mode;
                        if (mc.mode == "maze")
                        {
                            Maze.MakeMazeForHut(hut);
                        }
                        else
                        {
                            Maze.ClearMazeForHut(hut);
                        }
                    }
                }
            }
        }

        private void OnLaunched(object sender, GameLaunchedEventArgs e) {
            HutStates = new Dictionary<Vector2, HutState>();
            HutMazes = new Dictionary<Vector2, Maze>();
            
            Config = Helper.ReadConfig<ModConfig>();
            
            GMCMAPI = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            SetupGMCM();

            BJApi = Helper.ModRegistry.GetApi<IBetterJunimosApi>("hawkfalcon.BetterJunimos");
            if (BJApi is null) {
                Monitor.Log($"Could not load Better Junimos API", LogLevel.Error);
            }

            // BJApi.RegisterJunimoAbility(new Abilities.LayPathsAbility(Monitor));
            // Abilities now registered in OnSaveLoaded
        }

        private void SetupGMCM()
        {
            if (GMCMAPI is null)
            {
                Monitor.Log($"Could not load GMCM API", LogLevel.Error);
                return;
            }
            GMCMAPI.RegisterModConfig(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
            GMCMAPI.SetDefaultIngameOptinValue(ModManifest, true);

            GMCMAPI.RegisterSimpleOption(ModManifest, "Sustainable tree harvesting",
                "Only harvest wild trees when they've grown a seed", () => Config.SustainableWildTreeHarvesting,
                (val) => Config.SustainableWildTreeHarvesting = val);

            GMCMAPI.RegisterChoiceOption(ModManifest, "Wild tree pattern", "", () => Config.WildTreePattern,
                (string val) => Config.WildTreePattern = val, Config.WildTreePatternChoices);
            GMCMAPI.RegisterChoiceOption(ModManifest, "Fruit tree pattern", "", () => Config.FruitTreePattern,
                (string val) => Config.FruitTreePattern = val, Config.FruitTreePatternChoices);

            GMCMAPI.RegisterClampedOption(ModManifest, "Wild tree growth boost", "", () => Config.PlantWildTreesSize,
                (float val) => Config.PlantWildTreesSize = (int) val, 0, 5, 1);
            GMCMAPI.RegisterClampedOption(ModManifest, "Fruit tree growth boost", "", () => Config.PlantFruitTreesSize,
                (float val) => Config.PlantFruitTreesSize = (int) val, 0, 5, 1);

            GMCMAPI.RegisterSimpleOption(ModManifest, "Harvest Grass", "", () => Config.HarvestGrassEnabled,
                (val) => Config.HarvestGrassEnabled = val);
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnSaveLoaded(object sender, EventArgs e) {
            // reload the config to pick up any changes made in GMCM on the title screen
            Config = Helper.ReadConfig<ModConfig>();

            
            // we use these elsewhere
            PlantTrees = new Abilities.PlantTreesAbility(Monitor);
            PlantFruitTrees = new Abilities.PlantFruitTreesAbility();

            BJApi.RegisterJunimoAbility(new Abilities.HarvestGrassAbility());
            BJApi.RegisterJunimoAbility(new Abilities.HarvestDebrisAbility(Monitor));
            BJApi.RegisterJunimoAbility(new Abilities.CollectDroppedObjectsAbility(Monitor));
            BJApi.RegisterJunimoAbility(new Abilities.ChopTreesAbility(Monitor));
            BJApi.RegisterJunimoAbility(new Abilities.CollectSeedsAbility(Monitor));
            BJApi.RegisterJunimoAbility(new Abilities.FertilizeTreesAbility());
            BJApi.RegisterJunimoAbility(PlantTrees);
            BJApi.RegisterJunimoAbility(PlantFruitTrees);
            BJApi.RegisterJunimoAbility(new Abilities.HarvestFruitTreesAbility(Monitor));
            BJApi.RegisterJunimoAbility(new Abilities.HoeAroundTreesAbility(Monitor));
            
            
            if (!Context.IsMainPlayer) {
                Monitor.Log("Better Junimos Forestry is a single-player mod. It has not been tested in multi-player mode", LogLevel.Warn);
                return;
            }

            // load hut mode settings from the save file
            HutStates = Helper.Data.ReadSaveData<Dictionary<Vector2, HutState>>("ceruleandeep.BetterJunimosForestry.HutStates") ??
                        new Dictionary<Vector2, HutState>();

            // load hut maze settings from the save file
            HutMazes = Helper.Data.ReadSaveData<Dictionary<Vector2, Maze>>("ceruleandeep.BetterJunimosForestry.HutMazes") ??
                       new Dictionary<Vector2, Maze>();
        }
        
        /// <summary>Raised after a the game is saved</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnSaving(object sender, SavingEventArgs e) {
            Helper.Data.WriteSaveData("ceruleandeep.BetterJunimosForestry.HutStates", HutStates);
            Helper.Data.WriteSaveData("ceruleandeep.BetterJunimosForestry.HutMazes", HutMazes);
            Helper.WriteConfig(Config);
        }
        
        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnDayStarted(object sender, DayStartedEventArgs e) {
            foreach (JunimoHut hut in Game1.getFarm().buildings.OfType<JunimoHut>()) {
                if (Util.GetModeForHut(hut) == Modes.Maze)
                {
                    Maze.MakeMazeForHut(hut);
                }
            }

            // reset for rainy days, winter, or GMCM options change
            Helper.Content.InvalidateCache(@"Characters\Junimo");
        }
        
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        public override void Entry(IModHelper helper) {
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Display.RenderedWorld += RenderedWorld;
            Helper.Events.GameLoop.GameLaunched += OnLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.Saving += OnSaving;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;

            SMonitor = Monitor;
        }
    }
}