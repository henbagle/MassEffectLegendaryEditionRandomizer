﻿using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Unreal.Collections;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Shared.Classes
{

    /// <summary>
    /// Helper class for dealing with Gestures
    /// </summary>
    class GestureManager
    {
#if __GAME1__
        public static readonly string[] RandomGesturePackages =
        {

        };
#endif
#if __GAME2__
        public static readonly string[] RandomGesturePackages =
        {
            "HMM_DP_ArmsCross",
            "HMM_AM_Towny",
            "HMF_AM_Towny",
            "HMM_DL_HandChop",
            "HMM_DL_Smoking",
            "HMM_FC_Angry",
            // A lot more need to be added from the list
            
            "HMM_AM_DLC_FistPoundGlass",
            "HMM_WI_CocktailParty",
            "HMM_AM_HandsClap",
            "HMM_FC_Injuried", // Yes injuried
            "HMM_2P_ElectroTorture",
        };
#endif


#if __GAME3__
        public static readonly string[] RandomGesturePackages =
        {
            // KID ANIMATION
            "HMC_AM_Scared",
            "HMC_AM_MoveStartStop",
            "HMC_DP_Crawl",
            "HMC_AM_StandingDefault",

            // CHOKE FIGHT
            "HMF_2P_Choke",
            "HMF_2P_GarrusRomance",
            "HMF_2P_Main", // has kiss

            // "HMF_2P_PHT_SynchMelee", // NOT IN GESTURES

            "HMF_DP_BackAgainstWall",
            "HMF_DP_ArmsCrossed",
            "HMF_DP_BarActions",
            "HMF_DP_HandsFace",

            "HMF_FC_Communicator",
            "HMF_FC_Custom",

            // "HMM_2P_BAN_Synch",
            // "HMM_2P_ATA_Synch",
            "HMM_2P_ChokeLift",
            "HMM_2P_Choking",
            "HMM_2P_Comraderie",
            "HMM_2P_Consoling",
            "HMM_2P_Conspire",
            "HMM_2P_End002",
            "HMM_2P_ForcedExit",
            "HMM_2P_Grab",

            "HMM_2P_GunInterrupt",
            "HMM_2P_Handshake",
            "HMM_2P_HeadButt",
            "HMM_2P_HoldingHands",
            "HMM_2P_Hostage",
            "HMM_2P_InjuredAgainstWall",
            "HMM_2P_KaiLengDeath",
            "HMM_2P_KissCheek",
            "HMM_2P_KissMale",
            "HMM_2P_LiftPillar",
            "HMM_2P_Main",
            // "HMM_2P_PHT_SyncMelee",
            "HMM_2P_PinAgainstWall",
            "HMM_2P_PunchInterrupt",
            "HMM_2P_ThaneKaiLengFight",
            //"HMM_AM_BeckonPistol",
            //"HMM_AM_BeckonRifle",
            "HMM_AM_Biotic",
            "HMM_AM_Environmental",
            "HMM_AM_Gamble",
            "HMM_AM_HandsClap",
            "HMM_DG_Deaths",
            "HMM_DG_Exploration",
            "HMM_DL_Decline",
            "HMM_DL_ElusiveMan",
            "HMM_DL_EmoStates",
            "HMM_DL_Gestures",
            "HMM_DL_HandChop",
            "HMM_DL_HandDismiss",
            "HMM_DL_HenchActions",
            "HMM_DL_Melee",
            "HMM_DL_PoseBreaker",
            "HMM_DL_Smoking",
            "HMM_DL_Sparring",
            "HMM_DL_StandingDefault",
            "HMM_DP_ArmsCross",
            "HMM_DP_ArmsCrossedBack",
            "HMM_DP_BarActions",
            "HMM_DP_CatchDogTags",
            "HMM_DP_ChinTouch",
            "HMM_DP_ClenchFist",
            "HMM_DP_HandOnHip",
            "HMM_DP_HandsBehindBack",
            "HMM_DP_HandsFace",
            "HMM_DP_Salute",
            "HMM_DP_Shuttle",
            "HMM_DP_ShuttleTurbulence",
            "HMM_DP_ToughGuy",
            "HMM_DP_Whisper",
            "HMM_FC_Angry",
            "HMM_FC_Communicator",
            "HMM_FC_DesignerCutscenes",
            "HMM_FC_Main",
            "HMM_FC_Sad",
            "HMM_FC_Startled",
            "NCA_ELC_EX_AnimSet",
            "NCA_VOL_DL_AnimSet",
            "PTY_EX_Geth",
            "PTY_EX_Asari",
            "PTY_EX_Krogan",
            //"RPR_BAN_CB_Banshe",
            // "RPR_HSK_AM_Husk",
            // "RPR_HSK_CB_2PMelee", // Is in special .RPR subpackage, will need special handling
            // "RPR_HSK_CB_Husk",
            "YAH_SBR_CB_AnimSet",
            "HMF_AM_Towny", // Dance?
            "HMM_AM_Towny", // Dance
            "HMM_AM_ThinkingFrustration",
            "HMM_AM_Talk",
            "HMF_AM_Talk",
            "HMM_WI_Exercise", // Situp
            "HMM_AM_EatSushi", // Citadel Act I
            "HMF_AM_Party", // Citadel Party
            "HMM_AM_SurrenderPrisoner", // citadel
            "2P_EscapeToDeath", // citadel
            "2P_AM_Kitchen",
            "2P_AM_PushUps",
            "HMF_2P_GarrusShepardTango",
            "2P_BigKiss",
            "HMM_DP_SitFloorInjured",
            "HMM_AM_Possession",
        };
#endif

        /// <summary>
        /// Maps a name of an animation package to the actual unreal package name it sits under in packages
        /// </summary>
        private static Dictionary<string, string> mapAnimSetOwners;
        public static void Init(GameTarget target, bool loadGestures = true)
        {
            MERLog.Information("Initializing GestureManager");
            // Load gesture mapping (LE2 and LE3 use this)
            var gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfigDLC.pcc");
            if (!File.Exists(gesturesFile))
            {
                gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfig.pcc");
            }

            var gesturesPackage = MERFileSystem.OpenMEPackage(gesturesFile, preventSave: true);
            // name can change if it's dlc so we just do this
            var gestureRuntimeData = gesturesPackage.Exports.FirstOrDefault(x => x.ClassName == "BioGestureRuntimeData");
            var gestMap = ObjectBinary.From<BioGestureRuntimeData>(gestureRuntimeData);

            // Map it for strings since we don't want NameReferences.
            // Also load gestures cache
            _gesturePackageCache = new MERPackageCache(target, null, true);
            mapAnimSetOwners = new Dictionary<string, string>(gestMap.m_mapAnimSetOwners.Count);
            foreach (var v in gestMap.m_mapAnimSetOwners)
            {
                // Key = Animation set name
                // Value = Containing package
                mapAnimSetOwners[v.Key] = v.Value;
                if (loadGestures && RandomGesturePackages.Contains(v.Key.Name))
                {
                    MERLog.Information($"Preloading gesture package {v.Value.Name}.pcc");
                    _gesturePackageCache.GetCachedPackageEmbedded(target.Game,
                        $"Gestures.{v.Value.Name}.pcc"); // We don't capture the result - we just preload
                }
                else
                {
#if DEBUG
                    // MERLog.Warning($@"Gesture package not used: {v.Key.Name}");
#endif
                }
            }

#if DEBUG
            foreach (var p in _gesturePackageCache.GetPackageList())
            {
                Debug.WriteLine(p.FilePath);
            }
#endif
        }

        /// <summary>
        /// Determines if the listed object path matches a key in the gesture mapping values (the result value)
        /// </summary>
        /// <param name="instancedFullPath">The path of the object to check against</param>
        /// <returns></returns>
        public static bool IsGestureGroupPackage(string instancedFullPath)
        {
            return mapAnimSetOwners.Values.Any(x => x.Equals(instancedFullPath, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Cache holding the gesture packages in memory
        /// </summary>
        private static MERPackageCache _gesturePackageCache;

        /// <summary>
        /// Gets the package for the specified gesture group
        /// </summary>
        /// <param name="gestureGroupName"></param>
        /// <returns></returns>
        public static IMEPackage GetGesturePackage(string gestureGroupName)
        {
            if (mapAnimSetOwners.TryGetValue(gestureGroupName, out var packageName))
            {
                return _gesturePackageCache.GetCachedPackage($"Gestures.{packageName}.pcc", false);
            }
            Debug.WriteLine($"PACKAGE NOT FOUND IN GESTURE MAP {gestureGroupName}");
            return null;
        }

        /// <summary>
        /// Gets a random looping gesture. Can return null
        /// </summary>
        /// <returns></returns>
        public static GestureInfo GetRandomLoopingGesture()
        {
            int retryCount = 10;
            while (retryCount > 0)
            {
                retryCount--;

                IMEPackage randomGesturePackage = null;
                string gestureGroup = null;
                while (randomGesturePackage == null)
                {
                    gestureGroup = RandomGesturePackages.RandomElement();
                    randomGesturePackage = GetGesturePackage(gestureGroup);
                }
                var candidates = randomGesturePackage.Exports.Where(x => x.ClassName == "AnimSequence" && x.ParentName == mapAnimSetOwners[gestureGroup]
                                                                                                       && x.ObjectName.Name.StartsWith(gestureGroup + "_")
                                                                                                       && !x.ObjectName.Name.StartsWith(gestureGroup + "_Alt") // This is edge case for animation names
                                                                                                       ).ToList();
                var randGesture = candidates.RandomElement();

                // Get animations that loop.
                if (randGesture.ObjectName.Name.Contains("Exit", StringComparison.InvariantCultureIgnoreCase) ||
                    randGesture.ObjectName.Name.Contains("Enter", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // Make sure it has the right animgroup - some are subsets of another - e.g. ArmsCrossed and ArmsCrossed_Alt


                return new GestureInfo()
                {
                    GestureAnimSequence = randGesture,
                    GestureGroup = gestureGroup
                };
            }

            return null;
        }

        /// <summary>
        /// Generates a new BioDynamicAnimSet skelMeshComp under the specified parent
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <param name="group"></param>
        /// <param name="seq"></param>
        /// <param name="animSetData"></param>
        /// <returns></returns>
        public static ExportEntry GenerateBioDynamicAnimSet(GameTarget target, ExportEntry parent, GestureInfo gestInfo, bool isKismet = false)
        {
            // The incoming gestinfo might be pointing to the cached embedded version.
            // We look up the value in the given package to ensure we use the right values.

            var animSeq = parent.FileRef.FindExport(gestInfo.GestureAnimSequence.InstancedFullPath);
            var animSet = animSeq.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(parent.FileRef);

            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(gestInfo.GestureGroup, "m_nmOrigSetName"));
            props.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(animSeq) }, "Sequences"));
            props.Add(new ObjectProperty(animSet, "m_pBioAnimSetData"));

            BioDynamicAnimSet bin = new BioDynamicAnimSet()
            {
                SequenceNamesToUnkMap = new UMultiMap<NameReference, int>(1)
                {
                    {gestInfo.GestureName, 1} // If we ever add support for multiple we do it here.
                }
            };

            var rop = new RelinkerOptionsPackage() { Cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true) };
            var bioDynObj = new ExportEntry(parent.FileRef, parent, parent.FileRef.GetNextIndexedName(isKismet ? $"KIS_DYN_{gestInfo.GestureGroup}" : "BioDynamicAnimSet"), properties: props, binary: bin)
            {
                Class = EntryImporter.EnsureClassIsInFile(parent.FileRef, "BioDynamicAnimSet", rop)
            };

            // These will always be unique
            if (isKismet)
            {
                bioDynObj.indexValue = 0;
            }
            parent.FileRef.AddExport(bioDynObj);
            return bioDynObj;
        }

        public static Gesture InstallRandomFilteredGestureAsset(GameTarget target, IMEPackage targetPackage,
            float minLength = 0, string[] filterKeywords = null, string[] blacklistedKeywords = null,
            string[] mainPackagesAllowed = null, bool includeSpecial = false, MERPackageCache cache2 = null)
        {
            //var gestureFiles = MERUtilities.ListStaticPackageAssets(target, "Gestures", false, true);

            //// Special and package file filtering
            //// This is ME2R specific, might need reworked or removed for LE2R
            //if (mainPackagesAllowed != null)
            //{
            //    var newList = new List<string>();
            //    foreach (var gf in gestureFiles)
            //    {
            //        if (includeSpecial && gf.Contains("gestures.special."))
            //        {
            //            newList.Add(gf);
            //            continue;
            //        }

            //        var packageName = Path.GetFileNameWithoutExtension(MEREmbedded.GetFilenameFromAssetName(gf));
            //        if (mainPackagesAllowed.Contains(packageName))
            //        {
            //            newList.Add(gf);
            //            continue;
            //        }
            //    }

            //    gestureFiles = newList;
            //}

            // Pick a random package
            var randomPackageList = _gesturePackageCache.GetPackageList();
            randomPackageList.Shuffle();
            for (int i = randomPackageList.Count; i > 0; i--)
            {
                var gPackage = randomPackageList.PullFirstItem();
                List<ExportEntry> options;
                if (filterKeywords != null && blacklistedKeywords != null)
                {
                    options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                          && x.ObjectName.Name.ContainsAny(
                                                              StringComparison.InvariantCultureIgnoreCase, filterKeywords)
                                                          && !x.ObjectName.Name.ContainsAny(blacklistedKeywords)).ToList();
                }
                else if (filterKeywords != null)
                {
                    options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                          && x.ObjectName.Name.ContainsAny(
                                                              StringComparison.InvariantCultureIgnoreCase, filterKeywords))
                        .ToList();
                }
                else if (blacklistedKeywords != null)
                {
                    options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                          && !x.ObjectName.Name.ContainsAny(blacklistedKeywords)).ToList();
                }
                else
                {
                    options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence").ToList();
                }

                // remove non-gesture config entries
                options.RemoveAll(x => x.ObjectName.Name == "AnimSequence");

                // Ensure options are in config map
                // var fileName = Path.GetFileNameWithoutExtension(MEREmbedded.GetFilenameFromAssetName(randGestureFile)); // BIOG_HMM_DP_A

                if (options.Any())
                {
                    // Pick a random element
                    var randomGestureExport = options.RandomElement();

                    // Filter it out if we cannot use it
                    var seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");

                    int numRetries = 7;
                    while (seqLength < minLength && numRetries >= 0)
                    {
                        randomGestureExport = options.RandomElement();
                        seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");
                        numRetries--;
                    }

                    var portedInExp = PackageTools.PortExportIntoPackage(target, targetPackage, randomGestureExport);

                    return new Gesture(portedInExp);
                }
            }

            // Oh damn - nothing matches at all!
            Debugger.Break();
            return null;
        }

        /// <summary>
        /// Installs a dynamic anim set into a skeletal mesh component
        /// </summary>
        /// <param name="skelMeshComp"></param>
        /// <param name="gesture"></param>
        public static void InstallDynamicAnimSetRefForSkeletalMesh(ExportEntry skelMeshComp, Gesture gesture)
        {
            // We have parent sequence data
            var skmDynamicAnimSets = skelMeshComp.GetProperty<ArrayProperty<ObjectProperty>>("AnimSets") ?? new ArrayProperty<ObjectProperty>("AnimSets");

            // Check to see if there is any item that uses our bioanimset
#if __GAME1__
            IEntry bioAnimSet = null; // This needs worked out because we don't have a package mapping for this game
            throw new NotImplementedException();

#elif __GAME2__
            var bioAnimSet = gesture.GetBioAnimSet(skelMeshComp.FileRef, Game2.Misc.Game2Gestures.GestureSetNameToPackageExportName);
#elif __GAME3__
            var bioAnimSet = gesture.GetBioAnimSet(skelMeshComp.FileRef, Game3Gestures.GestureSetNameToPackageExportName);
#endif
            if (bioAnimSet != null)
            {
                ExportEntry skmBioDynamicAnimSet = null;
                foreach (var skmDynAnimSet in skmDynamicAnimSets)
                {
                    var kEntry = skmDynAnimSet.ResolveToEntry(skelMeshComp.FileRef) as ExportEntry; // I don't think these can be imports as they're part of the seq
                    var associatedset = kEntry.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(skelMeshComp.FileRef);
                    if (associatedset == bioAnimSet)
                    {
                        // It's this one
                        skmBioDynamicAnimSet = kEntry;
                        break;
                    }
                }

                if (skmBioDynamicAnimSet == null)
                {
                    // We need to generate a new one
                    PropertyCollection props = new PropertyCollection();
                    props.Add(new NameProperty(gesture.GestureSet, "m_nmOrigSetName"));
                    props.Add(new ArrayProperty<ObjectProperty>("Sequences"));
                    props.Add(new ObjectProperty(bioAnimSet, "m_pBioAnimSetData"));
                    skmBioDynamicAnimSet = ExportCreator.CreateExport(skelMeshComp.FileRef, $"BioDynamicAnimSet", "BioDynamicAnimSet", skelMeshComp);

                    // Write a blank count of 0 - we will update this in subsequent call
                    // This must be here to ensure parser can read it
                    skmBioDynamicAnimSet.WritePropertiesAndBinary(props, new byte[4]);
                    skmDynamicAnimSets.Add(new ObjectProperty(skmBioDynamicAnimSet)); // Add new skelMeshComp to sequence's list of biodynamicanimsets
                    skelMeshComp.WriteProperty(skmDynamicAnimSets);
                }

                var currentObjs = skmBioDynamicAnimSet.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                if (currentObjs.All(x => x.Value != gesture.Entry.UIndex))
                {
                    // We need to add our item to it
                    currentObjs.Add(new ObjectProperty(gesture.Entry));
                    var bin = ObjectBinary.From<BioDynamicAnimSet>(skmBioDynamicAnimSet);
                    bin.SequenceNamesToUnkMap[gesture.GestureAnim] = 1; // Not sure what the value should be, or if game actually reads this
                                                                        // FIX IT IF WE EVER FIGURE IT OUT!
                    skmBioDynamicAnimSet.WriteProperty(currentObjs);
                    skmBioDynamicAnimSet.WriteBinary(bin);
                }
            }
        }

        public static IEntry GetBioAnimSet(Gesture gesture, IMEPackage containingPackage)
        {
            return gesture.GetBioAnimSet(containingPackage, mapAnimSetOwners);
        }
    }
}
