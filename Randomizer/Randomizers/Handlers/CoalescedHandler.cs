﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Xml;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Config;
using Randomizer.MER;
using Randomizer.Randomizers.Shared.Classes;

namespace Randomizer.Randomizers.Handlers
{
    class CoalescedHandler
    {
        #region Static Access
        private static CoalescedHandler CurrentHandler { get; set; }
        private ConfigAssetBundle ConfigBundle { get; set; }
        /// <summary>
        /// Starts up the Coalesced handling system. These methods should not be across multiple threads as they are not thread safe!
        /// </summary>
        public static void StartHandler(MEGame game)
        {
            CurrentHandler = new CoalescedHandler();
            CurrentHandler.Start(game);
        }

        public static CoalesceAsset GetIniFile(string filename)
        {
            return CurrentHandler.GetFile(Path.GetFileNameWithoutExtension(filename));
        }

        public static void EndHandler()
        {
            // Commit
            CurrentHandler?.Commit();
            CurrentHandler = null;
        }

        #endregion

        #region Private members
        private void Start(MEGame game)
        {
            ConfigBundle = ConfigAssetBundle.FromDLCFolder(game, MERFileSystem.DLCModCookedPath, $"DLC_MOD_{game}Randomizer");
        }

        private CoalesceAsset GetFile(string filename)
        {
            return ConfigBundle.GetAsset(filename, true);
        }

        private void Commit()
        {
#if __GAME1__
            // Create M3CD
#else
            ConfigBundle.CommitDLCAssets();
#endif
        }
        #endregion

        public static void AddDynamicLoadMappingEntries(IEnumerable<CoalesceValue> mappings)
        {
            var engine = CoalescedHandler.GetIniFile("BioEngine");
            var sfxengine = engine.GetOrAddSection("SFXGame.SFXEngine");
            sfxengine.AddEntry(new CoalesceProperty("DynamicLoadMapping", mappings.ToList()));
        }

        public static void AddDynamicLoadMappingEntry(SeekFreeInfo mapping)
        {
            var engine = CoalescedHandler.GetIniFile("BioEngine");
            var sfxengine = engine.GetOrAddSection("SFXGame.SFXEngine");
            sfxengine.AddEntry(new CoalesceProperty("DynamicLoadMapping", new CoalesceValue(mapping.GetSeekFreeStructText(), CoalesceParseAction.AddUnique)));
        }

#if __GAME2__ || __GAME3__
        /// <summary>
        /// Adds a bool to the plot table that is not saved to disk
        /// </summary>
        /// <param name="boolIdx"></param>
        public static void AddMemoryBool(int boolIdx)
        {
            var game = CoalescedHandler.GetIniFile("BioGame");
            var gvTable = game.GetOrAddSection("SFXGame.BioGlobalVariableTable");
            gvTable.AddEntry(new CoalesceProperty("TimedPlotUnlocks", new CoalesceValue($"(PlotBool={boolIdx}, UnlockDay=0)", CoalesceParseAction.AddUnique)));
        }
#endif

        /// <summary>
        /// Adds a bool to config with Type 2 to enable/disable runtime randomizers
        /// </summary>
        public static void EnableFeatureFlag(string featureFlagName, bool enabled = true)
        {
            var game = CoalescedHandler.GetIniFile("BioEngine");
            var controlEngine = game.GetOrAddSection("Engine.MERControlEngine");
            controlEngine.AddEntry(new CoalesceProperty(featureFlagName, new CoalesceValue(enabled ? "TRUE" : "FALSE", CoalesceParseAction.Add)));
        }

        /// <summary>
        /// Sets a property in MERControlEngine
        /// </summary>
        /// <param name="boolIdx"></param>
        public static void SetProperty(CoalesceProperty prop)
        {
            var game = CoalescedHandler.GetIniFile("BioEngine");
            var controlEngine = game.GetOrAddSection("Engine.MERControlEngine");
            controlEngine.AddEntry(prop);
        }

    }
}
