﻿#if __GAME1__
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Memory;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Misc;
using ME3TweaksCore.NativeMods;
using Randomizer.MER;
using Randomizer.Randomizers.Game1._2DA;
using Randomizer.Randomizers.Game1.ExportTypes;
using Randomizer.Randomizers.Game1.Levels;
using Randomizer.Randomizers.Game1.Misc;
using Randomizer.Randomizers.Game1.TextureAssets.LE1;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Levels;
using Randomizer.Randomizers.Shared;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;
using Serilog;

namespace Randomizer.Randomizers.Game1
{
    public class Randomizer
    {
        private BackgroundWorker randomizationWorker;

        // Files that should not be generally passed over
        private static List<string> SpecializedFiles { get; } = new List<string>()
        {
            "EntryMenu",
            //"BioD_Nor_103aGalaxyMap",
            "BioG_UIWorld" // Char creator lighting
        };

        public Randomizer()
        {

        }

        /// <summary>
        /// Are we busy randomizing?
        /// </summary>
        public bool Busy => randomizationWorker != null && randomizationWorker.IsBusy;

        /// <summary>
        /// The options selected by the user that will be used to determine what the randomizer does
        /// </summary>
        public OptionsPackage SelectedOptions { get; set; }

        public void Randomize(OptionsPackage op)
        {
            SelectedOptions = op;
            ThreadSafeRandom.Reset();
            if (!SelectedOptions.UseMultiThread)
            {
                ThreadSafeRandom.SetSingleThread(SelectedOptions.Seed);
            }

            randomizationWorker = new BackgroundWorker();
            randomizationWorker.DoWork += PerformRandomization;
            randomizationWorker.RunWorkerCompleted += Randomization_Completed;

            if (SelectedOptions.UseMultiThread)
            {
                MERLog.Information("-------------------------STARTING RANDOMIZER (MULTI THREAD)--------------------------");
            }
            else
            {
                MERLog.Information($"------------------------STARTING RANDOMIZER WITH SEED {op.Seed} (SINGLE THREAD)--------------------------");
            }
            randomizationWorker.RunWorkerAsync();
            op.SetTaskbarState?.Invoke(MTaskbarState.Indeterminate);
        }

        private void Randomization_Completed(object? sender, RunWorkerCompletedEventArgs e)
        {
            // not implemented
            SelectedOptions?.SetRandomizationInProgress?.Invoke(false);
        }


        //        private void Randomization_Completed(object sender, RunWorkerCompletedEventArgs e)
        //        {
        //            if (e.Error != null)
        //            {
        //                MERLog.Exception(e.Error, @"Randomizer thread exited with exception!");
        //                SelectedOptions.SetCurrentOperationText?.Invoke($"Randomizer failed with error: {e.Error.Message}, please report to Mgamerz");
        //                TelemetryInterposer.TrackError(new Exception("Randomizer thread exited with exception", e.Error));
        //            }
        //            else
        //            {
        //                TelemetryInterposer.TrackEvent("Randomization completed");
        //                SelectedOptions.SetCurrentOperationText?.Invoke("Randomization complete");
        //            }
        //            CommandManager.InvalidateRequerySuggested();
        //            SelectedOptions.SetTaskbarState?.Invoke(MTaskbarState.None);
        //            SelectedOptions.SetRandomizationInProgress?.Invoke(false);
        //        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            MemoryManager.SetUsePooledMemory(true, false, false, (int)FileSize.KibiByte * 8, 4, 2048, false);
            ResetClasses(false);
            SelectedOptions.SetRandomizationInProgress?.Invoke(true);
            SelectedOptions.SetCurrentOperationText?.Invoke("Initializing randomizer");
            SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(true);
            var specificRandomizers = SelectedOptions.SelectedOptions.Where(x => x.PerformSpecificRandomizationDelegate != null).ToList();
            var perFileRandomizers = SelectedOptions.SelectedOptions.Where(x => x.PerformFileSpecificRandomization != null).ToList();
            var perExportRandomizers = SelectedOptions.SelectedOptions.Where(x => x.IsExportRandomizer).ToList();

            // MERLog randomizers
            MERLog.Information("Randomizers used in this pass:");
            foreach (var sr in specificRandomizers.Concat(perFileRandomizers).Concat(perExportRandomizers).Distinct())
            {
                MERLog.Information($" - {sr.HumanName}");
                if (sr.SubOptions != null)
                {
                    foreach (var subR in sr.SubOptions)
                    {
                        MERLog.Information($"   - {subR.HumanName}");
                    }
                }
            }

            Exception rethrowException = null;
            try
            {
                // Initialize FileSystem and handlers
                MERFileSystem.InitMERFS(SelectedOptions);

                // Initialize the global cache system
                MERCaches.Init(SelectedOptions.RandomizationTarget);

                // Install ASIs required to make game work with DLC
                var asiList = ASIManager.MasterLE1ASIUpdateGroups;
                ASIManager.InstallASIToTarget(asiList.Find(x => x.UpdateGroupId == ASIModIDs.LE1_AUTOTOC), SelectedOptions.RandomizationTarget);
                ASIManager.InstallASIToTarget(asiList.Find(x => x.UpdateGroupId == ASIModIDs.LE1_AUTOLOAD_ENABLER), SelectedOptions.RandomizationTarget);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Load any special initializions here

                // Prepare the textures
                LE1Textures.SetupLE1Textures(SelectedOptions.RandomizationTarget);

                SharedRandomizer.InventoryCustomKismetClasses();

                void srUpdate(object? o, EventArgs eventArgs)
                {
                    if (o is RandomizationOption option)
                    {
                        SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(option.ProgressIndeterminate);
                        SelectedOptions.SetOperationProgressBarProgress?.Invoke(option.ProgressValue, option.ProgressMax);
                        if (option.CurrentOperation != null)
                        {
                            SelectedOptions.SetCurrentOperationText?.Invoke(option.CurrentOperation);
                        }
                    }
                }

                // Pass 1: All randomizers that are file specific and are not post-run
                foreach (var sr in specificRandomizers.Where(x => !x.IsPostRun))
                {
                    sr.OnOperationUpdate += srUpdate;
                    MERLog.Information($"Running specific randomizer {sr.HumanName}");
                    SelectedOptions.SetCurrentOperationText?.Invoke($"Randomizing {sr.HumanName}");
                    sr.PerformSpecificRandomizationDelegate?.Invoke(SelectedOptions.RandomizationTarget, sr);
                    sr.OnOperationUpdate -= srUpdate;
                }

                // Pass 2: All exports
                if (perExportRandomizers.Any() || perFileRandomizers.Any())
                {
                    SelectedOptions.SetCurrentOperationText?.Invoke("Getting list of files...");
                    SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(true);

                    // we only want pcc files (me2/me3). no upks
                    var files = MELoadedFiles.GetFilesLoadedInGame(SelectedOptions.RandomizationTarget.Game, true, false, false, SelectedOptions.RandomizationTarget.TargetPath).Values.Where(x => !MERFileSystem.filesToSkip.Contains(Path.GetFileName(x))).ToList();

                    SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(false);

                    var currentFileNumber = 0;
                    var totalFilesCount = files.Count;

#if !DEBUG
                    Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = SelectedOptions.UseMultiThread ? 4 : 1 }, (file) =>
#else
                    Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = SelectedOptions.UseMultiThread ? 1 : 1 }, (file) =>
#endif
                    {

                        var name = Path.GetFileNameWithoutExtension(file);
                        if (SpecializedFiles.Contains(name, StringComparer.InvariantCultureIgnoreCase)) return; // Do not run randomization on this file as it's only done by specialized randomizers (e.g. char creator)

                        Interlocked.Increment(ref currentFileNumber);
                        SelectedOptions.SetOperationProgressBarProgress?.Invoke(currentFileNumber, totalFilesCount);
                        SelectedOptions.SetCurrentOperationText?.Invoke($"Randomizing game files [{currentFileNumber}/{files.Count}]");

#if DEBUG
                        if (true
                //&& false //uncomment to disable filtering
                && !file.Contains("GLO", StringComparison.InvariantCultureIgnoreCase)
                && !file.Contains("PRO", StringComparison.InvariantCultureIgnoreCase)
                && !file.Contains("NOR", StringComparison.InvariantCultureIgnoreCase)
                && !file.Contains("STA", StringComparison.InvariantCultureIgnoreCase)
                )
                            return;
#endif
                        // Skip NON INT
                        var localization = name.GetUnrealLocalization();
                        if (localization != MELocalization.None && localization != MELocalization.INT)
                            return;

                        try
                        {
                            //Log.Information($@"Opening package {file}");
                            var package = MERFileSystem.OpenMEPackage(file);
                            //Debug.WriteLine(file);
                            foreach (var rp in perFileRandomizers)
                            {
                                // Specific randomization pass before the exports are processed
                                rp.PerformFileSpecificRandomization(SelectedOptions.RandomizationTarget, package, rp);

                            }

                            if (perExportRandomizers.Any())
                            {
                                for (int i = 0; i < package.ExportCount; i++)
                                //                    foreach (var exp in package.Exports.ToList()) //Tolist cause if we add export it will cause modification
                                {
                                    var exp = package.Exports[i];
                                    foreach (var r in perExportRandomizers)
                                    {
                                        r.PerformRandomizationOnExportDelegate(SelectedOptions.RandomizationTarget, exp, r);
                                    }
                                }
                            }

                            MERFileSystem.SavePackage(package);
                        }
                        catch (Exception e)
                        {
                            MERLog.Error($@"Exception occurred in per-file/export randomization: {e.Message}");
                            TelemetryInterposer.TrackError(new Exception("Exception occurred in per-file/export randomizer", e));
                            Debugger.Break();
                        }
                    });
                }


                // Pass 3: All randomizers that are file specific and are not post-run
                foreach (var sr in specificRandomizers.Where(x => x.IsPostRun))
                {
                    try
                    {
                        sr.OnOperationUpdate += srUpdate;
                        SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(true);
                        MERLog.Information($"Running post-run specific randomizer {sr.HumanName}");
                        SelectedOptions.SetCurrentOperationText?.Invoke($"Randomizing {sr.HumanName}");
                        sr.PerformSpecificRandomizationDelegate?.Invoke(SelectedOptions.RandomizationTarget, sr);
                        sr.OnOperationUpdate -= srUpdate;
                    }
                    catch (Exception ex)
                    {
                        TelemetryInterposer.TrackError(new Exception($"Exception occurred in post-run specific randomizer {sr.HumanName}", ex));
                    }
                }

                sw.Stop();
                MERLog.Information($"Randomization time: {sw.Elapsed.ToString()}");

                SelectedOptions.SetOperationProgressBarIndeterminate?.Invoke(true);
                SelectedOptions.SetCurrentOperationText?.Invoke("Finishing up");
                SelectedOptions.NotifyDLCComponentInstalled?.Invoke(true);
            }
            catch (Exception exception)
            {
                MERLog.Exception(exception, "Unhandled Exception in randomization thread:");
                rethrowException = exception;
            }

            // Close out files and free memory
            CoalescedHandler.EndHandler();
            TLKBuilder.EndHandler();
            MERFileSystem.Finalize(SelectedOptions);
            ResetClasses(true);
            MemoryManager.ResetMemoryManager();
            MemoryManager.SetUsePooledMemory(false);

            // Re-throw the unhandled exception after MERFS has closed
            if (rethrowException != null)
                throw rethrowException;
        }

        /// <summary>
        /// Ensures things are set back to normal before first run
        /// </summary>
        private void ResetClasses(bool endOfRandomizationRun)
        {
            SharedRandomizer.CleanupInstallTimeOnlyFiles();
            MERCaches.Cleanup(endOfRandomizationRun);
        }


        /// <summary>
        /// Sets the options up that can be selected and their methods they call
        /// </summary>
        /// <param name="RandomizationGroups"></param>
        public static void SetupOptions(ObservableCollectionExtended<RandomizationGroup> RandomizationGroups, Action<RandomizationOption> optionChangingDelegate)
        {
#if DEBUG
            //EnemyPowerChanger.Init(null); // Load the initial list
            //EnemyWeaponChanger.Preboot(); // Load the initial list
#endif
            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Faces",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
#if DEBUG
                    //new RandomizationOption()
                    //{
                    //    Description="Runs debug code randomization",
                    //    HumanName = "Debug randomizer",
                    //    PerformRandomizationOnExportDelegate = DebugTools.DebugRandomizer.RandomizeExport,
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    //},
#endif
                    new RandomizationOption()
                    {
                        Description="Changes facial animation. The best feature of MER",
                        HumanName = "FaceFX animation", Ticks = "1,2,3,4,5", HasSliderOption = true, IsRecommended = true, SliderToTextConverter = rSetting =>
                            rSetting switch
                            {
                                1 => "Oblivion",
                                2 => "Knights of the old Republic",
                                3 => "Sonic Adventure",
                                4 => "Source filmmaker",
                                _ => "Error"
                            },
                        SliderTooltip = "Higher settings yield more extreme facial animation values. Default value is Sonic Adventure",
                        SliderValue = 3, // This must come after the converter
                        PerformRandomizationOnExportDelegate = RSharedFaceFXAnimSet.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    /*new RandomizationOption() {HumanName = "Squadmate heads",
                        Description = "Changes the heads of your squadmates",
                        PerformRandomizationOnExportDelegate = SquadmateHead.RandomizeExport2,
                        PerformFileSpecificRandomization = SquadmateHead.FilePrerun,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        RequiresTLK = true,
                        StateChangingDelegate=optionChangingDelegate,
                        MutualExclusiveSet = "SquadHead",
                        IsRecommended = true
                    },*/
                    //new RandomizationOption() {HumanName = "Squadmate faces",
                    //    Description = "Only works on Wilson and Jacob, unfortunately. Other squadmates are fully modeled",
                    //    PerformSpecificRandomizationDelegate = RBioMorphFace.RandomizeSquadmateFaces,
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    //    MutualExclusiveSet = "SquadHead",
                    //    StateChangingDelegate=optionChangingDelegate,
                    //},

                    new RandomizationOption()
                    {
                        HumanName = "NPC faces",
                        Ticks = "0.1,0.2,0.3,0.4,0.5,0.6,0.7",
                        HasSliderOption = true,
                        IsRecommended = true,
                        SliderTooltip = "Higher settings yield more ridiculous faces for characters that use the BioFaceMorph system. Default value is 0.3. Does not affect Garrus.",
                        SliderToTextConverter = rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = .3, // This must come after the converter
                        PerformRandomizationOnExportDelegate = RBioMorphFace.RandomizeExport,
                        Description="Changes the BioFaceMorph used by most pawns",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    },
                    // Sadly not used by anything but shepard
                    // For some reason data is embedded into files even though it's never used there
                    //new RandomizationOption()
                    //{
                    //    HumanName = "NPC Faces - Extra jacked up",
                    //    Description = "Changes the MorphTargets that map bones to the face morph system",
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    //    PerformRandomizationOnExportDelegate = RMorphTarget.RandomizeGlobalExport
                    //},
                    new RandomizationOption() {HumanName = "Eyes",
                        Description="Changes the colors of eyes",
                        IsRecommended = true,
                        PerformRandomizationOnExportDelegate = RSharedEyes.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    }
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Characters",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {

                    new RandomizationOption()
                    {
                        HumanName = "Animation Set Bones",
                        PerformRandomizationOnExportDelegate = RSharedBioAnimSetData.RandomizeExport,
                        SliderToTextConverter = RSharedBioAnimSetData.UIConverter,
                        HasSliderOption = true,
                        SliderValue = 1,
                        Ticks = "1,2,3,4,5",
                        SliderTooltip = "Higher settings yield more bone randomization. Default value basic bones only.",
                        Description = "Changes the order of animations mapped to bones. E.g. arm rotation will be swapped with eyes",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal
                    },
                    /*
                    new RandomizationOption() {HumanName = "NPC colors", Description="Changes NPC colors such as skin tone, hair, etc",
                        PerformRandomizationOnExportDelegate = RMaterialInstance.RandomizeNPCExport2,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal, IsRecommended = true},
                    new RandomizationOption() {HumanName = "NPC hair", Description="Randomizes the hair on NPCs that have a hair mesh",
                        PerformRandomizationOnExportDelegate = NPCHair.RandomizeExport,
                        PerformSpecificRandomizationDelegate = NPCHair.Init,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                    new RandomizationOption() {
                        HumanName = "Romance",
                        Description="Randomizes which romance you will get",
                        PerformSpecificRandomizationDelegate = Romance.PerformRandomization,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning, IsRecommended = true},
                    new RandomizationOption() {
                        HumanName = "Look At Definitions",
                        Description="Changes how pawns look at things",
                        PerformRandomizationOnExportDelegate = RBioLookAtDefinition.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {
                        HumanName = "Look At Targets",
                        Description="Changes where pawns look",
                        PerformRandomizationOnExportDelegate = RBioLookAtTarget.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },*/
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Character Creator",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    /*
                    new RandomizationOption() {
                        HumanName = "Premade faces",
                        IsRecommended = true,
                        Description = "Completely randomizes settings including skin tones and slider values. Adds extra premade faces",
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizeCharacterCreator,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                SubOptionKey = CharacterCreator.SUBOPTIONKEY_CHARCREATOR_NO_COLORS,
                                HumanName = "Don't randomize colors",
                                Description = "Prevents changing colors such as skin tone, teeth, eyes, etc",
                                Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                                IsOptionOnly = true
                            }
                        }
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Iconic FemShep face",
                        Description="Changes the default FemShep face",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        Ticks = "0.1,0.2,0.3,0.4,0.5,0.6,0.7",
                        HasSliderOption = true,
                        IsRecommended = true,
                        SliderTooltip = "Higher settings yield more extreme facial changes. Default value is 0.3.",
                        SliderToTextConverter = rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = .3, // This must come after the converter
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizeIconicFemShep
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Iconic MaleShep face",
                        Description="Changes the bones in default MaleShep face. Due to it being modeled, the changes only occur when the face moves",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        Ticks = "0.25,0.5,1.0,1.25,1.5,2.0",
                        HasSliderOption = true,
                        IsRecommended = true,
                        SliderTooltip = "Higher settings yields further bone position shifting, which can sometimes be undesirable. Default value is 1.0.",
                        SliderToTextConverter = rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = 1.0, // This must come after the converter
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizeIconicMaleShep,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                SubOptionKey = CharacterCreator.SUBOPTIONKEY_MALESHEP_COLORS,
                                HumanName = "Include colors",
                                Description = "Also changes colors like skintone, eyes, scars",
                                Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                                IsOptionOnly = true
                            }
                        }
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Psychological profiles",
                        Description="Completely changes the backstories of Shepard, with both new stories and continuations from ME1 Randomizer's stories",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        IsRecommended = true,
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizePsychProfiles,
                        RequiresTLK = true
                    },*/
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Miscellaneous",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    /*
                    new RandomizationOption() {HumanName = "Hologram colors", Description="Changes colors of holograms",PerformRandomizationOnExportDelegate = RHolograms.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Drone colors", Description="Changes colors of drones",PerformRandomizationOnExportDelegate = CombatDrone.RandomizeExport, IsRecommended = true},
                    //new RandomizationOption() {HumanName = "Omnitool", Description="Changes colors of omnitools",PerformRandomizationOnExportDelegate = ROmniTool.RandomizeExport},
                    new RandomizationOption() {HumanName = "Specific textures",Description="Changes specific textures to more fun ones", PerformRandomizationOnExportDelegate = TFCBuilder.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {HumanName = "SizeSixteens mode",
                        Description = "This option installs a change specific for the streamer SizeSixteens. If you watched his ME1 Randomizer streams, you'll understand the change.",
                        PerformSpecificRandomizationDelegate = SizeSixteens.InstallSSChanges,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        RequiresTLK = true
                    },
                    new RandomizationOption() {HumanName = "NPC names",
                        Description = "Install a list of names into the game and renames some of the generic NPCs to them. You can install your stream chat members, for example. There are 48 name slots.",
                        PerformSpecificRandomizationDelegate = CharacterNames.InstallNameSet,
                        SetupRandomizerDelegate = CharacterNames.SetupRandomizer,
                        SetupRandomizerButtonText = "Setup",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        RequiresTLK = true
                    },
#if DEBUG
                    new RandomizationOption() {
                        HumanName = "Skip splash",
                        Description = "Skips the splash screen",
                        PerformSpecificRandomizationDelegate = EntryMenu.SetupFastStartup,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        OptionIsSelected = true
                    },
#endif
                    */
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Movement & pawns",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "NPC movement speeds", Description = "Changes non-player movement stats. Can make combat very easy or very difficult", PerformRandomizationOnExportDelegate = RMovementSpeed2DA.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning, IsRecommended = true},
                    //new RandomizationOption() {HumanName = "Player movement speeds", Description = "Changes player movement stats", PerformSpecificRandomizationDelegate = PawnMovementSpeed.RandomizePlayerMovementSpeed, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                    new RandomizationOption() {HumanName = "NPC walking routes", PerformRandomizationOnExportDelegate = RBioWaypointSet.RandomizeExport},
                    //new RandomizationOption() {HumanName = "Hammerhead", IsRecommended = true, Description = "Changes HammerHead stats",PerformSpecificRandomizationDelegate = HammerHead.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                   // new RandomizationOption() {HumanName = "'Lite' pawn animations", IsRecommended = true, Description = "Changes the animations used by basic non-interactable NPCs. Some may T-pose due to the sheer complexity of this randomizer",PerformRandomizationOnExportDelegate = RSFXSkeletalMeshActorMAT.RandomizeBasicGestures, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning},
                    new RandomizationOption()
                    {
                        HumanName = "Pawn sizes", Description = "Changes the size of characters. Will break a lot of things, including bullet collision", PerformRandomizationOnExportDelegate = RBioPawn.RandomizePawnSize,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP,
                        Ticks = "0.1,0.2,0.3,0.4,0.5,0.75",
                        HasSliderOption = true,
                        SliderTooltip = "Values are added +/- to 1 to generate the range of allowed sizes. For example, 0.1 yields 90-110% size multiplier. Default value is 0.1.",
                        SliderToTextConverter = x=> $"Maximum size change: {Math.Round(x * 100)}%",
                        SliderValue = 0.1,
                    },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Weapons & Enemies",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    /*
                    new RandomizationOption() {HumanName = "Weapon stats", Description = "Attempts to change gun stats in a way that makes game still playable", PerformSpecificRandomizationDelegate = Weapons.RandomizeWeapons, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Usable weapon classes", Description = "Changes what guns the player and squad can use", PerformSpecificRandomizationDelegate = Weapons.RandomizeSquadmateWeapons, IsRecommended = true},
                    //new RandomizationOption() {HumanName = "Enemy AI", Description = "Changes enemy AI so they behave differently", PerformRandomizationOnExportDelegate = PawnAI.RandomizeExport, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Enemy weapons",
                        Description = "Gives enemies different guns",
                        // CHANGE TO EQUIPMENT 2DA SCAN GENERIC
                        PerformRandomizationOnExportDelegate = EnemyWeapon.RandomizeExport,
                        PerformSpecificRandomizationDelegate = EnemyWeaponChanger.Init,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                        IsRecommended = true,
                        // Debug stuff.
#if DEBUG
                        //HasSliderOption = true,
                        //Ticks = string.Join(",",Enumerable.Range(-1,EnemyWeaponChanger.AllAvailableWeapons.Count + 1)),
                        //SliderToTextConverter = x =>
                        //{
                        //    if (x < 0)
                        //        return "All weapons";
                        //    var idx = (int) x;
                        //    return EnemyWeaponChanger.AllAvailableWeapons[idx].GunName;
                        //},
                        //SliderValue = -1, // End debug stuff
#endif


                    },
                    new RandomizationOption()
                    {
                        // CHANGE TO ENEMY TALENT 2DA
                        HumanName = "Enemy powers", Description = "Gives enemies different powers", PerformRandomizationOnExportDelegate = EnemyPowerChanger.RandomizeExport, PerformSpecificRandomizationDelegate = EnemyPowerChanger.Init, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning, IsRecommended = true,
                        // Debug stuff.
#if DEBUG
                        //HasSliderOption = true,
                        //Ticks = string.Join(",",Enumerable.Range(-1,EnemyPowerChanger.Powers.Count + 1)),
                        //SliderToTextConverter = x =>
                        //{
                        //    if (x < 0)
                        //        return "All powers";
                        //    var idx = (int) x;
                        //    return EnemyPowerChanger.Powers[idx].PowerName;
                        //},
                        //SliderValue = -1, // End debug stuff
#endif
                    },*/
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level-specific",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Normandy", Description = "Changes various things around the ship, including one sidequest", PerformSpecificRandomizationDelegate = RNormandy.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    //new RandomizationOption() {HumanName = "Prologue"},
                    //new RandomizationOption() {HumanName = "Tali Acquisition"}, //sfxgame tla damagetype
                    new RandomizationOption() {HumanName = "Citadel", Description = "Changes many things across the level", PerformSpecificRandomizationDelegate = RCitadel.PerformRandomization, RequiresTLK = true, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Feros", Description = "It's a mystery!", PerformSpecificRandomizationDelegate = RFeros.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true, RequiresTLK = true},
                    new RandomizationOption() {HumanName = "Noveria", Description = "Changes the lounge", PerformSpecificRandomizationDelegate = RNoveria.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Opening Sequence", Description = "Improved dancing technique", PerformSpecificRandomizationDelegate = ROpeningSequence.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Eden Prime", Description = "Significantly changes level. Greatly increases difficulty", PerformSpecificRandomizationDelegate = REdenPrime.PerformRandomization, RequiresTLK = true, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Pinnacle Station", Description = "Significantly changes level. Greatly increases difficulty", PerformSpecificRandomizationDelegate = RPinnacleStation.PerformRandomization, RequiresTLK = true, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Bring Down The Sky", Description = "Significantly changes level. Greatly increases difficulty", PerformSpecificRandomizationDelegate = RBringDownTheSky.PerformRandomization, RequiresTLK = true, IsRecommended = true},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Squad powers",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    // Needs tested if this actually works in LE1. It won't work in ME1 due to hardcoded UI
                    //new RandomizationOption()
                    //{
                    //    HumanName = "Class powers",
                    //    Description = "Shuffles the powers of all player classes. Loading an existing save after running this will cause you to lose talent points. Use the refund points button below to adjust your latest save file and reset your powers.",
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    //    IsRecommended = true,
                    //    PerformSpecificRandomizationDelegate = ClassTalents.ShuffleClassAbilitites,
                    //    RequiresTLK = true,
                    //    SetupRandomizerDelegate = HenchTalents.ResetTalents,
                    //    SetupRandomizerButtonToolTip = "Allows you to select a save file to remove player power records from.\nThis will wipe all assigned power points and refund the correct amount of talent points to spend.",
                    //    SetupRandomizerButtonText = "Refund points",
                    //    /* Will have to implement later as removing gating code is actually complicated
                    //    SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                    //    {
                    //        new RandomizationOption()
                    //        {
                    //            IsOptionOnly = true,
                    //            SubOptionKey = HenchTalents.SUBOPTION_HENCHPOWERS_REMOVEGATING,
                    //            HumanName = "Remove rank-up gating",
                    //            Description = "Removes the unlock requirement for the second power slot. The final power slot will still be gated by loyalty."
                    //        }
                    //    }*/
                    //},

#if DEBUG
                    // Needs fixes in porting code...
                    /*new RandomizationOption()
                    {
                        HumanName = "Henchmen powers",
                        Description = "Shuffles the powers of squadmates. Loading an existing save after running this will cause them to lose talent points. Use the refund points button below to adjust your latest save file and reset their powers.",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                        IsRecommended = true,
                        PerformSpecificRandomizationDelegate = HenchTalents.ShuffleSquadmateAbilities,
                        SetupRandomizerDelegate = HenchTalents.ResetTalents,
                        SetupRandomizerButtonToolTip = "Allows you to select a save file to remove henchman records from.\nThis will wipe all henchman powers and refund the correct amount of talent points to spend.\nThis will ALSO reset the weapon they are using the default weapon they use.",
                        SetupRandomizerButtonText = "Refund points",
                        RequiresTLK = true,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                IsOptionOnly = true,
                                SubOptionKey = HenchTalents.SUBOPTION_HENCHPOWERS_REMOVEGATING,
                                HumanName = "Remove rank-up gating",
                                Description = "Removes the unlock requirement for the second power slot. The final power slot will still be gated by loyalty."
                            }
                        }
                    },*/
#endif
                }
            });


            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Gameplay",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption()
                    {
                        HumanName = "Enable basic friendly fire",
                        PerformSpecificRandomizationDelegate = SFXGame.TurnOnFriendlyFire,
                        Description = "Enables weapons to damage non hostile enemies. You can't directly aim at friendly NPCs.",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Battle Royale Mode",
                        PerformFileSpecificRandomization = RBattleRoyale.RandomizeFile,
                        Description = "Everyone that isn't important to the plot wakes up on the wrong side of the bed",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                    },
                    //new RandomizationOption()
                    //{
                    //    HumanName = "Shepard ragdollable",
                    //    Description = "Makes Shepard able to be ragdolled from various powers/attacks. Can greatly increase difficulty",
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                    //    PerformSpecificRandomizationDelegate = SFXGame.MakeShepardRagdollable,
                    //},
                    // TODO: REIMPLEMENT
                    //new RandomizationOption()
                    //{
                    //    HumanName = "Remove running camera shake",
                    //    Description = "Removes the camera shake when running",
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    //    PerformSpecificRandomizationDelegate = SFXGame.RemoveStormCameraShake,
                    //},
                    //new RandomizationOption()
                    //{
                    //    HumanName = "One hit kill",
                    //    Description = "Makes Shepard die upon taking any damage. Removes bonuses that grant additional health. Extremely difficult, do not mix with other randomizers",
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Unsafe,
                    //    PerformSpecificRandomizationDelegate = OneHitKO.InstallOHKO,
                    //},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level-components",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    // Doesn't seem to work
                    //                    new RandomizationOption() {HumanName = "Star colors", IsRecommended = true, PerformRandomizationOnExportDelegate = RBioSun.PerformRandomization},
                    new RandomizationOption() {
                        HumanName = "Fog colors",
                        Description = "Changes colors of fog",
                        IsRecommended = true,
                        PerformSpecificRandomizationDelegate = RSharedHeightFogComponent.InstallDynamicHeightFogRandomizer,
                        IsRuntimeRandomizer = true,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    new RandomizationOption() {
                        HumanName = "Post Processing volumes",
                        Description = "Changes postprocessing. Likely will make some areas of game unplayable",
                        PerformRandomizationOnExportDelegate = RPostProcessingVolume.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP
                    },
                    new RandomizationOption() {
                        HumanName = "Light colors",
                        Description = "Changes colors of dynamic lighting",
                        PerformSpecificRandomizationDelegate = RSharedLighting.InstallDynamicLightingRandomizer,
                        IsRecommended = true,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    new RandomizationOption() {
                        HumanName = "Clusters",
                        Description = "Randomizes positions of clusters",
                        PerformRandomizationOnExportDelegate = RGalaxyMapClusters2DA.RandomizeClustersXY,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Text",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Game over text", PerformSpecificRandomizationDelegate = RSharedTexts.RandomizeGameOverText, RequiresTLK = true, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption() {HumanName = "Intro Crawl", PerformSpecificRandomizationDelegate = RSharedTexts.RandomizeOpeningCrawl, RequiresTLK = true, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe, IsRecommended = true},
                    new RandomizationOption()
                    {
                        HumanName = "Vowels",
                        IsPostRun = true,
                        Description="Changes vowels in text in a consistent manner, making a 'new' language",
                        PerformSpecificRandomizationDelegate = RSharedTexts.RandomizeVowels,
                        RequiresTLK = true,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                        MutualExclusiveSet="AllText",
                        StateChangingDelegate=optionChangingDelegate,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                SubOptionKey = RSharedTexts.SUBOPTIONKEY_VOWELS_HARDMODE,
                                HumanName = "Hurd Medi",
                                Description = "Adds an additional 2 consonants to swap (for a total of 4 letter changes). Can make text extremely challenging to read",
                                Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP,
                                IsOptionOnly = true
                            }
                        }
                    },
                    new RandomizationOption() {
                        HumanName = "UwU",
                        Description="UwUifies all text in the game, often hilarious.",
                        PerformSpecificRandomizationDelegate = RSharedTexts.UwuifyText,
                        RequiresTLK = true,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        MutualExclusiveSet="AllText",
                        StateChangingDelegate=optionChangingDelegate,
                        IsPostRun = true,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                IsOptionOnly = true,
                                HumanName = "Keep casing",
                                Description = "Keeps upper and lower casing.",
                                SubOptionKey = RSharedTexts.SUBOPTIONKEY_UWU_KEEPCASING,
                            },
                            new RandomizationOption()
                            {
                                IsOptionOnly = true,
                                HumanName = "Emoticons",
                                Description = "Adds emoticons based on content of a sentence ^_^\n'Keep casing' recommended.",
                                Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                                SubOptionKey = RSharedTexts.SUBOPTIONKEY_REACTIONS_ENABLED
                            }
                        }
                    },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Wackadoodle",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {
                        HumanName = "Actors in cutscenes",
                        Description="Swaps pawns around in animated cutscenes",
                        PerformRandomizationOnExportDelegate = RSharedCutscene.ShuffleCutscenePawns,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal,
                        IsRecommended = true
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Actors in conversations",
                        PerformFileSpecificRandomization = RBioConversation.RandomizeActorsInConversation2,
                        Description = "Changes pawn roles in conversations",
                        IsRecommended = false,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP
                    },
                    // Due to how stage placement works this doesn't really work very well in this game.
                    //new RandomizationOption() {
                    //    HumanName = "Actors in conversations",
                    //    Description="Swaps pawns around in conversations that have more than 2 participants (rare in this game).",
                    //    PerformRandomizationOnExportDelegate = RBioConversation.RandomizeExport,
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                    //    IsRecommended = true
                    //},
                    new RandomizationOption() {
                            HumanName = "Animation data",
                            PerformRandomizationOnExportDelegate = RSharedAnimSequence.RandomizeExport,
                            SliderToTextConverter = RSharedAnimSequence.UIConverter,
                            HasSliderOption = true,
                            SliderValue = 1,
                            Ticks = "1,2",
                            Description="Shifts rigged bone positions",
                            IsRecommended = true,
                            SliderTooltip = "Value determines which bones are used in the remapping. Default value is basic bones only.",
                            Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Random interpolations",
                        Description = "Randomly fuzzes interpolation data. Can make game very dizzying on higher values!",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP,
                        PerformRandomizationOnExportDelegate = RSharedInterpTrackMove.RandomizeExport,
                        Ticks = "0.025,0.05,0.075,0.1,0.15,0.2,0.3,0.4,0.5",
                        HasSliderOption = true,
                        SliderTooltip = "Higher settings yield more extreme position and rotational changes to interpolations. Values above 0.05 are very likely to make the game unplayable. Default value is 0.05.",
                        SliderToTextConverter = x=> $"Maximum interp change: {Math.Round(x * 100)}%",
                        SliderValue = 0.05,
                    },
                    
                    /*new RandomizationOption()
                    {
                        HumanName = "Conversation Wheel", PerformRandomizationOnExportDelegate = RBioConversation.RandomizeExportReplies,
                        Description = "Changes replies in wheel. Can make conversations hard to exit",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Unsafe
                    },*/
                    //new RandomizationOption()
                    //{
                    //    HumanName = "Actors in conversations",
                    //    PerformFileSpecificRandomization = RBioConversation.RandomizeActorsInConversation,
                    //    Description = "Changes pawn roles in conversations. Somewhat buggy simply due to complexity and restrictions in engine, but can be entertaining",
                    //    IsRecommended = true,
                    //    Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning
                    //},

                    new RandomizationOption()
                    {
                        HumanName = "Music",
                        PerformRandomizationOnExportDelegate = RMusic2DA.RandomizeExport,
                        Description = "Changes what music is played in-game. Often plays unfitting music. This does not affect certain areas as some areas have audio not classified as 'Music'.",
                        IsRecommended = false,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning
                    }
                }
            });

            foreach (var g in RandomizationGroups)
            {
                g.Options.Sort(x => x.HumanName);
            }
            RandomizationGroups.Sort(x => x.GroupName);
        }
    }
}
#endif