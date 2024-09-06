using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using LethalLib;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System.Reflection;
using System.Collections.Generic;
using LethalConfig;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using UnityEngine.ProBuilder;
using System;
using Steamworks.Ugc;

namespace SmilingCritters
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency("ainavt.lc.lethalconfig")]
    public class SmilingCritters : BaseUnityPlugin
    {
        public static SmilingCritters Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        internal static AssetBundle assetBundle;

        //Config stuff
        internal enum RarityAddTypes { All, Modded, Vanilla, List };
        internal enum ScrapEnabled {True, False};

        internal RarityAddTypes defaultScrapAddingMethod = RarityAddTypes.All;

        internal string defaultMoonRegistrationList = "Experimentation, Assurance";


        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            //Try to load assets from asset package.
            LoadAssetBundle();
            ScrapConfig.RegisterAllScrap(Instance);
            CreatureConfig.RegisterAllCritters(Instance);
            
            //Unneeded in case we want to modify original game files, which I try to avoid unless needed.
            NetcodePatcher();
            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public void AdjustScrapValues(Dictionary<Item, int> itemDictionary, float adjustment)
        {
            if (AreEqual(adjustment, 1.0f))
            {
                return;
            }
            else
            {
                foreach (Item item in itemDictionary.Keys)
                {
                    item.minValue = (int)(item.minValue * adjustment);
                    item.maxValue = (int)(item.maxValue * adjustment);
                }
            }
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }

        private void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        //Loads the assets from the location this code is running from, combined with the name of the assetbundle.
        private void LoadAssetBundle()
        {
            string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "smilingcritters");
            assetBundle = AssetBundle.LoadFromFile(text);
            if (assetBundle != null)
            {
                Logger.LogMessage("Loaded SmilingCritters Assets");
            }
        }

        //Registers items as scrap using the item and rarity value for each item.  To be expanded upon later.
        internal void RegisterItemsAsScrap(Dictionary<Item, int> items, RarityAddTypes enumScrapRegistration, string levelOverrides)
        {
            LethalLib.Modules.Levels.LevelTypes chosenRegistrationEnum;

            switch (enumScrapRegistration)
            {
                case RarityAddTypes.All:
                    chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.All;
                    break;
                case RarityAddTypes.Vanilla:
                    chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.Vanilla;
                    break;
                case RarityAddTypes.Modded:
                    chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.Modded;
                    break;
                case RarityAddTypes.List:
                    chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.None;
                    break;
                default:
                    chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.All;
                    break;
            }
            foreach (KeyValuePair<Item, int> entry in items)
            {
                Item item = entry.Key;
                int rarity = entry.Value;

                //These are needed to register items on the "missing" vanilla levels listed in this string array.  For vanilla they need to be added, and modded they must be removed, as the game doesn't consider them vanilla but they are in fact vanilla.
                LethalLib.Modules.Levels.LevelTypes regEnum = LethalLib.Modules.Levels.LevelTypes.None;
                string[] tempStringArray = { "Adamance", "Embrion", "Artifice" };

                if (item != null)
                {
                    //All items need mixer groups fixed and to be registered to the network in order to work properly.
                    LethalLib.Modules.Utilities.FixMixerGroups(item.spawnPrefab);
                    LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
                    if (enumScrapRegistration == RarityAddTypes.List)
                    {
                        //Handles parsing the Overrides list to allow specific registration to moons of your choosing.
                        ExtractAndRegisterLevels(item, rarity, levelOverrides);
                    }
                    //If its vanilla only, we need to add the 3 mistakenly mislabeled vanilla moons.
                    else if (enumScrapRegistration == RarityAddTypes.Vanilla)
                    {
                        chosenRegistrationEnum = LethalLib.Modules.Levels.LevelTypes.Vanilla;
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, chosenRegistrationEnum, tempStringArray);
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, regEnum, tempStringArray);
                    }
                    else
                    {
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, chosenRegistrationEnum);

                        //If its modded only, we go ahead and register to all and then subtract the mistakenly mislabeled vanilla moons.
                        if (enumScrapRegistration == RarityAddTypes.Modded)
                        {
                            LethalLib.Modules.Items.RemoveScrapFromLevels(item, regEnum, tempStringArray);
                        }
                    }
                }
            }
        }

        //Registers each item to vanilla moons that are correctly added to the leveltypes enum.  Registering by this enum value is the only way to register them, as it doesn't work when trying by name.
        internal void ExtractAndRegisterLevels(Item item, int rarity, string allLevelOverrides)
        {
            LethalLib.Modules.Levels.LevelTypes regEnum = LethalLib.Modules.Levels.LevelTypes.None;

            foreach (string levelOverride in ParseLevelList(allLevelOverrides))
            {
                Logger.LogMessage("Trying to register " + item.itemName + " on " + levelOverride);
                switch (levelOverride.ToLower())
                {
                    case "experimentation":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.ExperimentationLevel);
                        break;
                    case "assurance":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.AssuranceLevel);
                        break;
                    case "offense":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.OffenseLevel);
                        break;
                    case "dine":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.DineLevel);
                        break;
                    case "titan":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.TitanLevel);
                        break;
                    case "march":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.MarchLevel);
                        break;
                    case "rend":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.RendLevel);
                        break;
                    case "vow":
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.VowLevel);
                        break;
                    default:
                        string capitalized = char.ToUpper(levelOverride[0]) + levelOverride.Substring(1);
                        string lowercased = levelOverride.ToLower();
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, regEnum, new string[] { capitalized });
                        LethalLib.Modules.Items.RegisterScrap(item, rarity, regEnum, new string[] { lowercased });
                        break;
                }
            }
        }

        //Splits the comma sperated list of moons you wish to add.
        internal string[] ParseLevelList(string levels)
        {
            return levels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(level => level.Trim())
                         .ToArray();
        }

        //Checks if all of the items were loaded correctly.
        internal void CheckAllItemsLoaded(Dictionary<Item, int> itemsToRegister)
        {
            bool allItemsLoaded = true;
            foreach (Item item in itemsToRegister.Keys)
            {
                if (item == null)
                {
                    allItemsLoaded = false;
                    break;
                }
            }
            if (allItemsLoaded)
            {
                Logger.LogMessage("All the smiling critters and toys are here!");
            }
            else
            {
                Logger.LogMessage("Some critters or toys are missing!");
            }
        }

        //Makes a integer slider configuration entry with LethalConfig with limits 0-100 for rarity of item spawned.
        internal ConfigEntry<int> CreateIntSliderConfig(string itemName, int defaultRarity, string description, int sliderMin, int sliderMax, string configCategory)
        {
            var rarityEntry = Config.Bind(configCategory, itemName, defaultRarity, description + " [" + sliderMin + "-" + sliderMax + "]");
            var slider = new IntSliderConfigItem(rarityEntry, new IntSliderOptions
            {
                RequiresRestart = true,
                Min = sliderMin,
                Max = sliderMax
            });
            LethalConfigManager.AddConfigItem(slider);
            return rarityEntry;
        }

        internal ConfigEntry<float> CreateFloatSliderConfig(string itemName, float defaultValue, string description, float sliderMin, float sliderMax, string configCategory)
        {
            var floatEntry = Config.Bind(configCategory, itemName, defaultValue, description + " [" + sliderMin + "-" + sliderMax + "]");
            var slider = new FloatSliderConfigItem(floatEntry, new FloatSliderOptions
            {
                RequiresRestart = true,
                Min = sliderMin,
                Max = sliderMax
            });
            LethalConfigManager.AddConfigItem(slider);
            return floatEntry;
        }

        public bool AreEqual(float a, float b, float tolerance = 0.0001f)
        {
            return System.Math.Abs(a - b) < tolerance;
        }

        /*Creates and configures the custom script NoiseMakerCycle.  It works almost the same as NoiseMaker in the base game except
         *it will cycle and remember which audio tracks it has played.  AudioClip 1 -> AudioClip 2 -> AudioClip3 and so forth.
         */
        internal void ConfigureNoiseMakerCycle(Item item, NoiseMakerCycle noiseMakerCycle, AssetBundle assetBundle)
        {
            //Generic stuff used for all NoiseMakerCycle instances, can be adjusted specifically later if needed.
            noiseMakerCycle.grabbable = true;
            noiseMakerCycle.isInFactory = true;
            noiseMakerCycle.grabbableToEnemies = true;
            noiseMakerCycle.itemProperties = item;
            noiseMakerCycle.noiseAudio = item.spawnPrefab.GetComponent<AudioSource>();
            noiseMakerCycle.noiseAudioFar = item.spawnPrefab.transform.Find("FarAudio").GetComponent<AudioSource>();
            noiseMakerCycle.useCooldown = 7f;
            noiseMakerCycle.noiseRange = 25;
            noiseMakerCycle.maxLoudness = 1f;
            noiseMakerCycle.minLoudness = 0.7f;
            noiseMakerCycle.minPitch = 0.94f;
            noiseMakerCycle.maxPitch = 0.98f;

            //Specific audio clips for each variant.
            if (item.itemName.Equals("Bobby Bearhug"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[6];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Bobby1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Bobby2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Bobby3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Bobby4");
                noiseMakerCycle.noiseSFX[4] = assetBundle.LoadAsset<AudioClip>("Bobby5");
                noiseMakerCycle.noiseSFX[5] = assetBundle.LoadAsset<AudioClip>("Bobby6");
                noiseMakerCycle.noiseSFXFar = new AudioClip[6];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Bobby1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Bobby2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Bobby3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Bobby4Far");
                noiseMakerCycle.noiseSFXFar[4] = assetBundle.LoadAsset<AudioClip>("Bobby5Far");
                noiseMakerCycle.noiseSFXFar[5] = assetBundle.LoadAsset<AudioClip>("Bobby6Far");
            }
            else if (item.itemName.Equals("Bubba Bubbaphant"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[3];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Bubba1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Bubba2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Bubba3");
                noiseMakerCycle.noiseSFXFar = new AudioClip[3];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Bubba1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Bubba2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Bubba3Far");
            }
            else if (item.itemName.Equals("Catnap"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[3];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Catnap1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Catnap2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Catnap3");
                noiseMakerCycle.noiseSFXFar = new AudioClip[3];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Catnap1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Catnap2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Catnap3Far");
            }
            else if (item.itemName.Equals("Craftycorn"))
            {
                noiseMakerCycle.useCooldown = 8f;
                noiseMakerCycle.noiseSFX = new AudioClip[5];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Crafty1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Crafty2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Crafty3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Crafty4");
                noiseMakerCycle.noiseSFX[4] = assetBundle.LoadAsset<AudioClip>("Crafty5");
                noiseMakerCycle.noiseSFXFar = new AudioClip[5];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Crafty1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Crafty2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Crafty3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Crafty4Far");
                noiseMakerCycle.noiseSFXFar[4] = assetBundle.LoadAsset<AudioClip>("Crafty5Far");
            }
            else if (item.itemName.Equals("Dogday"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[4];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Dogday1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Dogday2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Dogday3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Dogday4");
                noiseMakerCycle.noiseSFXFar = new AudioClip[4];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Dogday1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Dogday2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Dogday3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Dogday4Far");
            }
            else if (item.itemName.Equals("Hoppy Hopscotch"))
            {
                noiseMakerCycle.useCooldown = 8f;
                noiseMakerCycle.noiseSFX = new AudioClip[5];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Hoppy1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Hoppy2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Hoppy3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Hoppy4");
                noiseMakerCycle.noiseSFX[4] = assetBundle.LoadAsset<AudioClip>("Hoppy5");
                noiseMakerCycle.noiseSFXFar = new AudioClip[5];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Hoppy1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Hoppy2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Hoppy3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Hoppy4Far");
                noiseMakerCycle.noiseSFXFar[4] = assetBundle.LoadAsset<AudioClip>("Hoppy5Far");
            }
            else if (item.itemName.Equals("Kickin Chicken"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[5];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Kickin1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Kickin2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Kickin3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Kickin4");
                noiseMakerCycle.noiseSFX[4] = assetBundle.LoadAsset<AudioClip>("Kickin5");
                noiseMakerCycle.noiseSFXFar = new AudioClip[5];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Kickin1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Kickin2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Kickin3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Kickin4Far");
                noiseMakerCycle.noiseSFXFar[4] = assetBundle.LoadAsset<AudioClip>("Kickin5Far");
            }
            else if (item.itemName.Equals("Picky Piggy"))
            {
                noiseMakerCycle.noiseSFX = new AudioClip[6];
                noiseMakerCycle.noiseSFX[0] = assetBundle.LoadAsset<AudioClip>("Picky1");
                noiseMakerCycle.noiseSFX[1] = assetBundle.LoadAsset<AudioClip>("Picky2");
                noiseMakerCycle.noiseSFX[2] = assetBundle.LoadAsset<AudioClip>("Picky3");
                noiseMakerCycle.noiseSFX[3] = assetBundle.LoadAsset<AudioClip>("Picky4");
                noiseMakerCycle.noiseSFX[4] = assetBundle.LoadAsset<AudioClip>("Picky5");
                noiseMakerCycle.noiseSFX[5] = assetBundle.LoadAsset<AudioClip>("Picky6");
                noiseMakerCycle.noiseSFXFar = new AudioClip[6];
                noiseMakerCycle.noiseSFXFar[0] = assetBundle.LoadAsset<AudioClip>("Picky1Far");
                noiseMakerCycle.noiseSFXFar[1] = assetBundle.LoadAsset<AudioClip>("Picky2Far");
                noiseMakerCycle.noiseSFXFar[2] = assetBundle.LoadAsset<AudioClip>("Picky3Far");
                noiseMakerCycle.noiseSFXFar[3] = assetBundle.LoadAsset<AudioClip>("Picky4Far");
                noiseMakerCycle.noiseSFXFar[4] = assetBundle.LoadAsset<AudioClip>("Picky5Far");
                noiseMakerCycle.noiseSFXFar[5] = assetBundle.LoadAsset<AudioClip>("Picky6Far");
            }
            else
            {
                Logger.LogError("Item name not recognized for special NoiseMakerCycle configuration: " + item.itemName);
            }
        }
    }
}