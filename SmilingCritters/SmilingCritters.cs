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

        private RarityAddTypes defaultScrapAddingMethod = RarityAddTypes.All;

        private string defaultMoonRegistrationList = "Experimentation, Assurance";


        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            //Try to load assets from asset package.
            LoadAssetBundle();
            CreatureConfig.RegisterAllCritters();

            //Enumerated way to register scrap.
            var configEntryForScrapMethod = Config.Bind("Where to register scrap.", "Registration Method", defaultScrapAddingMethod
                , "The method to add scrap to the level. \n Default = All \n Vanilla \n Modded \n List");
            var enumScrapRegistration = new EnumDropDownConfigItem<RarityAddTypes>(configEntryForScrapMethod);

            //Default list to register scrap.
            var configEntryForScrapRegistrationList = Config.Bind("Where to register scrap.", "Registration List", defaultMoonRegistrationList,
                "The list of moons to register scrap.  Requires \"List\" to be selected in order to work.\n\n" +
                "Use a comma seperated list like this:\n assurance, rend \n Note: Registration will try both what you enter and with the first letter capitalized.  Assurance or assurance would work in your list for example.  If you had a moon named Mare it would also work with mare or Mare.");
            var moonRegistrationList = new TextInputFieldConfigItem(configEntryForScrapRegistrationList);

            //Get the rarity for each scrap from configs.  variable.Value returns the integer representing rarity.
            var rarityEntryBobbyBearHug = CreateIntSliderConfig("Bobby Bearhug", 20, "Adjust how often you see Bobby Bearhug.", 0, 100, "Item Rarity");
            var rarityEntryBubbaBubbaphant = CreateIntSliderConfig("Bubba Bubbaphant", 20, "Adjust how often you see Bubba Bubbaphant.", 0, 100, "Item Rarity");
            var rarityEntryCatnap = CreateIntSliderConfig("Catnap", 15, "Adjust how often you see Catnap.", 0, 100, "Item Rarity");
            var rarityEntryCraftyCorn = CreateIntSliderConfig("CraftyCorn", 20, "Adjust how often you see CraftyCorn.", 0, 100, "Item Rarity");
            var rarityEntryDogday = CreateIntSliderConfig("Dogday", 15, "Adjust how often you see Dogday.", 0, 100, "Item Rarity");
            var rarityEntryHoppyHopscotch = CreateIntSliderConfig("Hoppy Hopscotch", 20, "Adjust how often you see Hoppy Hopscotch.", 0, 100, "Item Rarity");
            var rarityEntryKickinChicken = CreateIntSliderConfig("Kickin Chicken", 20, "Adjust how often you see Kickin Chicken.", 0, 100, "Item Rarity");
            var rarityEntryPickyPiggy = CreateIntSliderConfig("Picky Piggy", 20, "Adjust how often you see Picky Piggy.", 0, 100, "Item Rarity");

            //Rarity config entries for toybox items
            var rarityEntryEmptyTier = CreateIntSliderConfig("Empty Toybox Items", 6, "Adjust how often you see empty toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryRuinedTier = CreateIntSliderConfig("Ruined Toybox Items", 6, "Adjust how often you see ruined toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryDamagedTier = CreateIntSliderConfig("Damaged Toybox Items", 7, "Adjust how often you see damaged toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryPristineTier = CreateIntSliderConfig("Pristine Toybox Items", 20, "Adjust how often you see pristine toyboxes or toys.", 0, 100, "Item Rarity");

            //Rarity config for generic items that don't fall into any particular category.
            var rarityEntryGenericScrap = CreateIntSliderConfig("Other items", 20, "Adjust how often you see generic scrap items related to Poppy Playtime.  Not related to toys.  \nA misc. category for things that aren't smiling critters or toys.", 0, 100, "Item Rarity");

            //Rarity config for golden statues.
            var rarityEntryGoldenStatues = CreateIntSliderConfig("Golden Statue Items", 2, "Adjust how often you see golden statues.", 0, 100, "Item Rarity");

            //Config to adjust the scrap value multiplier for items.
            var scrapValueConfigSmiling = CreateFloatSliderConfig("Smiling Critter scrap values", 1.0f, "Adjust the scrap value of the main 8 smiling critters.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigEmpty = CreateFloatSliderConfig("Empty toybox scrap values", 1.0f, "Adjust the scrap value of empty box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigRuined = CreateFloatSliderConfig("Ruined toybox values", 1.0f, "Adjust the scrap value of ruined box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigDamaged = CreateFloatSliderConfig("Damaged toybox scrap values", 1.0f, "Adjust the scrap value of damaged box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigPristineBox = CreateFloatSliderConfig("Pristine toybox scrap values", 1.0f, "Adjust the scrap value of pristine box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigPristineToy = CreateFloatSliderConfig("Pristine standalone toy values", 1.0f, "Adjust the scrap value of fully intact toys.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigGeneric = CreateFloatSliderConfig("Generic scrap values", 1.0f, "Adjust the scrap value of remaining generic misc items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigGolden = CreateFloatSliderConfig("Golden Statue values", 1.0f, "Adjust the scrap value of golden statues.", 0.25f, 2.5f, "Scrap Values");

            //Configure custom script to be added onto the items to cycle sounds.  Similar to NoiseMaker but cycles through sounds rather than selecting randomly.
            Item bobbyBearhug = assetBundle.LoadAsset<Item>("BobbyBearhugPlushie");
            NoiseMakerCycle bobbyNoiseMakerCycle = bobbyBearhug.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(bobbyBearhug, bobbyNoiseMakerCycle, assetBundle);

            Item bubbaBubbaphant = assetBundle.LoadAsset<Item>("BubbaBubbaphantPlushie");
            NoiseMakerCycle bubbaNoiseMakerCycle = bubbaBubbaphant.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(bubbaBubbaphant, bubbaNoiseMakerCycle, assetBundle);

            Item catNap = assetBundle.LoadAsset<Item>("CatNapPlushie");
            NoiseMakerCycle catNoiseMakerCycle = catNap.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(catNap, catNoiseMakerCycle, assetBundle);

            Item craftyCorn = assetBundle.LoadAsset<Item>("CraftyCornPlushie");
            NoiseMakerCycle craftyNoiseMakerCycle = craftyCorn.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(craftyCorn, craftyNoiseMakerCycle, assetBundle);

            Item dogDay = assetBundle.LoadAsset<Item>("DogDayPlushie");
            NoiseMakerCycle dogNoiseMakerCycle = dogDay.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(dogDay, dogNoiseMakerCycle, assetBundle);

            Item hoppyHopscotch = assetBundle.LoadAsset<Item>("HoppyHopscotchPlushie");
            NoiseMakerCycle hoppyNoiseMakerCycle = hoppyHopscotch.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(hoppyHopscotch, hoppyNoiseMakerCycle, assetBundle);

            Item kickinChicken = assetBundle.LoadAsset<Item>("KickinChickenPlushie");
            NoiseMakerCycle kickinNoiseMakerCycle = kickinChicken.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(kickinChicken, kickinNoiseMakerCycle, assetBundle);

            Item pickyPiggy = assetBundle.LoadAsset<Item>("PickyPiggyPlushie");
            NoiseMakerCycle pickyNoiseMakerCycle = pickyPiggy.spawnPrefab.AddComponent<NoiseMakerCycle>();
            ConfigureNoiseMakerCycle(pickyPiggy, pickyNoiseMakerCycle, assetBundle);

            //Load the generic scrap items.
            Item boxSmall = assetBundle.LoadAsset<Item>("Playco Box Small");
            Item boxMedium = assetBundle.LoadAsset<Item>("Playco Box Medium");
            Item boxLarge = assetBundle.LoadAsset<Item>("Playco Box Large");

            //Load the Toyboxes.
            //Empty Tier:
            Item boogieBotBoxEmpty = assetBundle.LoadAsset<Item>("Boogie Bot Box Empty");
            Item bronBoxEmpty = assetBundle.LoadAsset<Item>("Bron Box Empty");
            Item bunzoBunnyBoxEmpty = assetBundle.LoadAsset<Item>("Bunzo Bunny Box Empty");
            Item candyCatBoxEmpty = assetBundle.LoadAsset<Item>("Candy Cat Box Empty");
            Item catBeeBoxEmpty = assetBundle.LoadAsset<Item>("Cat Bee Box Empty");
            Item huggyWuggyBoxEmpty = assetBundle.LoadAsset<Item>("Huggy Wuggy Box Empty");
            Item mommyLongLegsBoxEmpty = assetBundle.LoadAsset<Item>("Mommy Long Legs Box Empty");
            Item PJPugAPillarBoxEmpty = assetBundle.LoadAsset<Item>("PJ Pug A Pillar Box Empty");

            //Ruined Tier:
            Item boogieBotBoxRuined = assetBundle.LoadAsset<Item>("Boogie Bot Box Ruined");
            Item bronBoxRuined = assetBundle.LoadAsset<Item>("Bron Box Ruined");
            Item bunzoBunnyBoxRuined = assetBundle.LoadAsset<Item>("Bunzo Bunny Box Ruined");
            Item candyCatBoxRuined = assetBundle.LoadAsset<Item>("Candy Cat Box Ruined");
            Item catBeeBoxRuined = assetBundle.LoadAsset<Item>("Cat Bee Box Ruined");
            Item mommyLongLegsBoxRuined = assetBundle.LoadAsset<Item>("Mommy Long Legs Box Ruined");
            Item PJPugAPillarBoxRuined = assetBundle.LoadAsset<Item>("PJ Pug A Pillar Box Ruined");

            //Damaged Tier:
            Item boogieBotBoxDamaged = assetBundle.LoadAsset<Item>("Boogie Bot Box Damaged");
            Item candyCatBoxDamaged = assetBundle.LoadAsset<Item>("Candy Cat Box Damaged");
            Item mommyLongLegsBoxDamaged = assetBundle.LoadAsset<Item>("Mommy Long Legs Box Damaged");

            //Pristine Tier:
            Item boogieBotBoxPristine = assetBundle.LoadAsset<Item>("Boogie Bot Box Pristine");
            Item bunzoBunnyBoxPristine = assetBundle.LoadAsset<Item>("Bunzo Bunny Box Pristine");
            Item candyCatBoxPristine = assetBundle.LoadAsset<Item>("Candy Cat Box Pristine");
            Item catBeeBoxPristine = assetBundle.LoadAsset<Item>("Cat Bee Box Pristine");
            Item huggyWuggyBoxPristine = assetBundle.LoadAsset<Item>("Huggy Wuggy Box Pristine");

            //Pristine Full Toy Standalone Tier:
            Item boogieBotPristine = assetBundle.LoadAsset<Item>("Boogie Bot Pristine");
            Item bronPristine = assetBundle.LoadAsset<Item>("Bron Pristine");
            Item bunzoBunnyPristine = assetBundle.LoadAsset<Item>("Bunzo Bunny Pristine");
            //Add the sound clip to Bunzo Bunny's clashing cymbals
            CymbalClashBunzoBunny cymbalScript = bunzoBunnyPristine.spawnPrefab.AddComponent<CymbalClashBunzoBunny>();
            cymbalScript.soundClip = assetBundle.LoadAsset<AudioClip>("BunzoCymbalHit");
            Item candyCatPristine = assetBundle.LoadAsset<Item>("Candy Cat Pristine");
            Item PJPugAPillarPristine = assetBundle.LoadAsset<Item>("PJ Pug A Pillar Pristine");

            //Golden Statue Items
            Item goldenBerryStatue = assetBundle.LoadAsset<Item>("Golden Berry Statue");
            Item goldenBunzoStatue = assetBundle.LoadAsset<Item>("Golden Bunzo Statue");
            Item goldenClawStatue = assetBundle.LoadAsset<Item>("Golden Claw Statue");
            Item goldenDaisyStatue = assetBundle.LoadAsset<Item>("Golden Daisy Statue");
            Item goldenHandStatue = assetBundle.LoadAsset<Item>("Golden Hand Statue");
            Item goldenKissyStatue = assetBundle.LoadAsset<Item>("Golden Kissy Statue");
            Item goldenMommyStatue = assetBundle.LoadAsset<Item>("Golden Mommy Statue");
            Item goldenPJStatue = assetBundle.LoadAsset<Item>("Golden PJ Statue");
            Item goldenTrainStatue = assetBundle.LoadAsset<Item>("Golden Train Statue");


            Dictionary<Item, int> smilingCritterItems = new Dictionary<Item, int>
            {
                {bobbyBearhug, rarityEntryBobbyBearHug.Value},
                {bubbaBubbaphant, rarityEntryBubbaBubbaphant.Value},
                {catNap, rarityEntryCatnap.Value},
                {craftyCorn, rarityEntryCraftyCorn.Value},
                {dogDay, rarityEntryDogday.Value},
                {hoppyHopscotch, rarityEntryHoppyHopscotch.Value},
                {kickinChicken, rarityEntryKickinChicken.Value},
                {pickyPiggy, rarityEntryPickyPiggy.Value}
            };

            Dictionary<Item, int> emptyItems = new Dictionary<Item, int>
            {
                {boogieBotBoxEmpty, rarityEntryEmptyTier.Value},
                {bronBoxEmpty, rarityEntryEmptyTier.Value},
                {bunzoBunnyBoxEmpty, rarityEntryEmptyTier.Value},
                {candyCatBoxEmpty, rarityEntryEmptyTier.Value},
                {catBeeBoxEmpty, rarityEntryEmptyTier.Value},
                {huggyWuggyBoxEmpty, rarityEntryEmptyTier.Value},
                {mommyLongLegsBoxEmpty, rarityEntryEmptyTier.Value},
                {PJPugAPillarBoxEmpty, rarityEntryEmptyTier.Value}
            };

            Dictionary<Item, int> ruinedItems = new Dictionary<Item, int>
            {
                {boogieBotBoxRuined, rarityEntryRuinedTier.Value},
                {bronBoxRuined, rarityEntryRuinedTier.Value},
                {bunzoBunnyBoxRuined, rarityEntryRuinedTier.Value},
                {candyCatBoxRuined, rarityEntryRuinedTier.Value},
                {catBeeBoxRuined, rarityEntryRuinedTier.Value},
                {mommyLongLegsBoxRuined, rarityEntryRuinedTier.Value},
                {PJPugAPillarBoxRuined, rarityEntryRuinedTier.Value}
            };

            Dictionary<Item, int> damagedItems = new Dictionary<Item, int>
            {
                {boogieBotBoxDamaged, rarityEntryDamagedTier.Value},
                {candyCatBoxDamaged, rarityEntryDamagedTier.Value},
                {mommyLongLegsBoxDamaged, rarityEntryDamagedTier.Value}
            };

            Dictionary<Item, int> pristineBoxItems = new Dictionary<Item, int>
            {
                {boogieBotBoxPristine, rarityEntryPristineTier.Value},
                {bunzoBunnyBoxPristine, rarityEntryPristineTier.Value},
                {candyCatBoxPristine, rarityEntryPristineTier.Value},
                {catBeeBoxPristine, rarityEntryPristineTier.Value},
                {huggyWuggyBoxPristine, rarityEntryPristineTier.Value}
            };

            Dictionary<Item, int> pristineToyItems = new Dictionary<Item, int>
            {
                {boogieBotPristine, rarityEntryPristineTier.Value},
                {bronPristine, rarityEntryPristineTier.Value},
                {bunzoBunnyPristine, rarityEntryPristineTier.Value},
                {candyCatPristine, rarityEntryPristineTier.Value},
                {PJPugAPillarPristine, rarityEntryPristineTier.Value}
            };

            Dictionary<Item, int> genericItems = new Dictionary<Item, int>
            {
                {boxSmall, rarityEntryGenericScrap.Value},
                {boxMedium, rarityEntryGenericScrap.Value},
                {boxLarge, rarityEntryGenericScrap.Value}
            };

            Dictionary<Item, int> goldenStatueItems = new Dictionary<Item, int> 
            {
                {goldenBerryStatue, rarityEntryGoldenStatues.Value},
                {goldenBunzoStatue, rarityEntryGoldenStatues.Value},
                {goldenClawStatue, rarityEntryGoldenStatues.Value},
                {goldenDaisyStatue, rarityEntryGoldenStatues.Value},
                {goldenHandStatue, rarityEntryGoldenStatues.Value},
                {goldenKissyStatue, rarityEntryGoldenStatues.Value},
                {goldenMommyStatue, rarityEntryGoldenStatues.Value},
                {goldenPJStatue, rarityEntryGoldenStatues.Value},
                {goldenTrainStatue, rarityEntryGoldenStatues.Value}
            };


            //Adjust scrap values if needed
            AdjustScrapValues(smilingCritterItems, scrapValueConfigSmiling.Value);
            AdjustScrapValues(emptyItems, scrapValueConfigEmpty.Value);
            AdjustScrapValues(ruinedItems, scrapValueConfigRuined.Value);
            AdjustScrapValues(damagedItems, scrapValueConfigDamaged.Value);
            AdjustScrapValues(pristineBoxItems, scrapValueConfigPristineBox.Value);
            AdjustScrapValues(pristineToyItems, scrapValueConfigPristineToy.Value);
            AdjustScrapValues(genericItems, scrapValueConfigGeneric.Value);
            AdjustScrapValues(goldenStatueItems, scrapValueConfigGolden.Value);

            //Register the scrap.
            RegisterItemsAsScrap(smilingCritterItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(emptyItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(ruinedItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(damagedItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(pristineBoxItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(pristineToyItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(genericItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            RegisterItemsAsScrap(goldenStatueItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            
            //Unneeded in case we want to modify original game files, which I try to avoid unless needed.
            NetcodePatcher();
            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private void AdjustScrapValues(Dictionary<Item, int> itemDictionary, float adjustment)
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
        private void RegisterItemsAsScrap(Dictionary<Item, int> items, RarityAddTypes enumScrapRegistration, string levelOverrides)
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
        private void ExtractAndRegisterLevels(Item item, int rarity, string allLevelOverrides)
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
        private string[] ParseLevelList(string levels)
        {
            return levels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(level => level.Trim())
                         .ToArray();
        }

        //Checks if all of the items were loaded correctly.
        private void CheckAllItemsLoaded(Dictionary<Item, int> itemsToRegister)
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

        private bool AreEqual(float a, float b, float tolerance = 0.0001f)
        {
            return System.Math.Abs(a - b) < tolerance;
        }

        /*Creates and configures the custom script NoiseMakerCycle.  It works almost the same as NoiseMaker in the base game except
         *it will cycle and remember which audio tracks it has played.  AudioClip 1 -> AudioClip 2 -> AudioClip3 and so forth.
         */
        private void ConfigureNoiseMakerCycle(Item item, NoiseMakerCycle noiseMakerCycle, AssetBundle assetBundle)
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