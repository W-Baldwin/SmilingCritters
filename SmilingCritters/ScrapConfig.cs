using System;
using LethalConfig.ConfigItems;
using System.Collections.Generic;
using System.Text;
using static SmilingCritters.SmilingCritters;
using System.Diagnostics;
using UnityEngine;

namespace SmilingCritters
{
    internal class ScrapConfig
    {
        private static bool scrapEnabled;

        internal static void RegisterAllScrap(SmilingCritters Instance)
        {
            var configEntryScrapEnabled = Instance.Config.Bind("Enabled", "Item Rarity", true
                , "Whether scrap is enabled or not.  Will override everything and disable or enable scrap. \n True by default");
            var enumCreatureRegistration = new BoolCheckBoxConfigItem(configEntryScrapEnabled, true);
            scrapEnabled = configEntryScrapEnabled.Value;
            //Enumerated way to register scrap.
            var configEntryForScrapMethod = Instance.Config.Bind("Where to register scrap.", "Registration Method", Instance.defaultScrapAddingMethod
                , "The method to add scrap to the level. \n Default = All \n Vanilla \n Modded \n List");
            var enumScrapRegistration = new EnumDropDownConfigItem<RarityAddTypes>(configEntryForScrapMethod);

            //Default list to register scrap.
            var configEntryForScrapRegistrationList = Instance.Config.Bind("Where to register scrap.", "Registration List", Instance.defaultMoonRegistrationList,
                "The list of moons to register scrap.  Requires \"List\" to be selected in order to work.\n\n" +
                "Use a comma seperated list like this:\n assurance, rend \n Note: Registration will try both what you enter and with the first letter capitalized.  Assurance or assurance would work in your list for example.  If you had a moon named Mare it would also work with mare or Mare.");
            var moonRegistrationList = new TextInputFieldConfigItem(configEntryForScrapRegistrationList);

            //Get the rarity for each scrap from configs.  variable.Value returns the integer representing rarity.
            var rarityEntryBobbyBearHug = Instance.CreateIntSliderConfig("Bobby Bearhug", 20, "Adjust how often you see Bobby Bearhug.", 0, 100, "Item Rarity");
            var rarityEntryBubbaBubbaphant = Instance.CreateIntSliderConfig("Bubba Bubbaphant", 20, "Adjust how often you see Bubba Bubbaphant.", 0, 100, "Item Rarity");
            var rarityEntryCatnap = Instance.CreateIntSliderConfig("Catnap", 15, "Adjust how often you see Catnap.", 0, 100, "Item Rarity");
            var rarityEntryCraftyCorn = Instance.CreateIntSliderConfig("CraftyCorn", 20, "Adjust how often you see CraftyCorn.", 0, 100, "Item Rarity");
            var rarityEntryDogday = Instance.CreateIntSliderConfig("Dogday", 15, "Adjust how often you see Dogday.", 0, 100, "Item Rarity");
            var rarityEntryHoppyHopscotch = Instance.CreateIntSliderConfig("Hoppy Hopscotch", 20, "Adjust how often you see Hoppy Hopscotch.", 0, 100, "Item Rarity");
            var rarityEntryKickinChicken = Instance.CreateIntSliderConfig("Kickin Chicken", 20, "Adjust how often you see Kickin Chicken.", 0, 100, "Item Rarity");
            var rarityEntryPickyPiggy = Instance.CreateIntSliderConfig("Picky Piggy", 20, "Adjust how often you see Picky Piggy.", 0, 100, "Item Rarity");

            //Rarity config entries for toybox items
            var rarityEntryEmptyTier = Instance.CreateIntSliderConfig("Empty Toybox Items", 6, "Adjust how often you see empty toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryRuinedTier = Instance.CreateIntSliderConfig("Ruined Toybox Items", 6, "Adjust how often you see ruined toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryDamagedTier = Instance.CreateIntSliderConfig("Damaged Toybox Items", 7, "Adjust how often you see damaged toyboxes or toys.", 0, 100, "Item Rarity");
            var rarityEntryPristineTier = Instance.CreateIntSliderConfig("Pristine Toybox Items", 20, "Adjust how often you see pristine toyboxes or toys.", 0, 100, "Item Rarity");

            //Rarity config for generic items that don't fall into any particular category.
            var rarityEntryGenericScrap = Instance.CreateIntSliderConfig("Other items", 20, "Adjust how often you see generic scrap items related to Poppy Playtime.  Not related to toys.  \nA misc. category for things that aren't smiling critters or toys.", 0, 100, "Item Rarity");

            //Rarity config for golden statues.
            var rarityEntryGoldenStatues = Instance.CreateIntSliderConfig("Golden Statue Items", 2, "Adjust how often you see golden statues.", 0, 100, "Item Rarity");

            //Config to adjust the scrap value multiplier for items.
            var scrapValueConfigSmiling = Instance.CreateFloatSliderConfig("Smiling Critter scrap values", 1.0f, "Adjust the scrap value of the main 8 smiling critters.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigEmpty = Instance.CreateFloatSliderConfig("Empty toybox scrap values", 1.0f, "Adjust the scrap value of empty box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigRuined = Instance.CreateFloatSliderConfig("Ruined toybox values", 1.0f, "Adjust the scrap value of ruined box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigDamaged = Instance.CreateFloatSliderConfig("Damaged toybox scrap values", 1.0f, "Adjust the scrap value of damaged box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigPristineBox = Instance.CreateFloatSliderConfig("Pristine toybox scrap values", 1.0f, "Adjust the scrap value of pristine box items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigPristineToy = Instance.CreateFloatSliderConfig("Pristine standalone toy values", 1.0f, "Adjust the scrap value of fully intact toys.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigGeneric = Instance.CreateFloatSliderConfig("Generic scrap values", 1.0f, "Adjust the scrap value of remaining generic misc items.", 0.25f, 2.5f, "Scrap Values");
            var scrapValueConfigGolden = Instance.CreateFloatSliderConfig("Golden Statue values", 1.0f, "Adjust the scrap value of golden statues.", 0.25f, 2.5f, "Scrap Values");

            if (!scrapEnabled)
            {
                SmilingCritters.Logger.LogMessage("Scrap not enabled, skipping scrap registration...");
                return;
            }

            //Configure custom script to be added onto the items to cycle sounds.  Similar to NoiseMaker but cycles through sounds rather than selecting randomly.
            Item bobbyBearhug = assetBundle.LoadAsset<Item>("BobbyBearhugPlushie");
            NoiseMakerCycle bobbyNoiseMakerCycle = bobbyBearhug.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(bobbyBearhug, bobbyNoiseMakerCycle, assetBundle);

            Item bubbaBubbaphant = assetBundle.LoadAsset<Item>("BubbaBubbaphantPlushie");
            NoiseMakerCycle bubbaNoiseMakerCycle = bubbaBubbaphant.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(bubbaBubbaphant, bubbaNoiseMakerCycle, assetBundle);

            Item catNap = assetBundle.LoadAsset<Item>("CatNapPlushie");
            NoiseMakerCycle catNoiseMakerCycle = catNap.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(catNap, catNoiseMakerCycle, assetBundle);

            Item craftyCorn = assetBundle.LoadAsset<Item>("CraftyCornPlushie");
            NoiseMakerCycle craftyNoiseMakerCycle = craftyCorn.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(craftyCorn, craftyNoiseMakerCycle, assetBundle);

            Item dogDay = assetBundle.LoadAsset<Item>("DogDayPlushie");
            NoiseMakerCycle dogNoiseMakerCycle = dogDay.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(dogDay, dogNoiseMakerCycle, assetBundle);

            Item hoppyHopscotch = assetBundle.LoadAsset<Item>("HoppyHopscotchPlushie");
            NoiseMakerCycle hoppyNoiseMakerCycle = hoppyHopscotch.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(hoppyHopscotch, hoppyNoiseMakerCycle, assetBundle);

            Item kickinChicken = assetBundle.LoadAsset<Item>("KickinChickenPlushie");
            NoiseMakerCycle kickinNoiseMakerCycle = kickinChicken.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(kickinChicken, kickinNoiseMakerCycle, assetBundle);

            Item pickyPiggy = assetBundle.LoadAsset<Item>("PickyPiggyPlushie");
            NoiseMakerCycle pickyNoiseMakerCycle = pickyPiggy.spawnPrefab.AddComponent<NoiseMakerCycle>();
            Instance.ConfigureNoiseMakerCycle(pickyPiggy, pickyNoiseMakerCycle, assetBundle);

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
            Instance.AdjustScrapValues(smilingCritterItems, scrapValueConfigSmiling.Value);
            Instance.AdjustScrapValues(emptyItems, scrapValueConfigEmpty.Value);
            Instance.AdjustScrapValues(ruinedItems, scrapValueConfigRuined.Value);
            Instance.AdjustScrapValues(damagedItems, scrapValueConfigDamaged.Value);
            Instance.AdjustScrapValues(pristineBoxItems, scrapValueConfigPristineBox.Value);
            Instance.AdjustScrapValues(pristineToyItems, scrapValueConfigPristineToy.Value);
            Instance.AdjustScrapValues(genericItems, scrapValueConfigGeneric.Value);
            Instance.AdjustScrapValues(goldenStatueItems, scrapValueConfigGolden.Value);

            //Register the scrap.
            Instance.RegisterItemsAsScrap(smilingCritterItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(emptyItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(ruinedItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(damagedItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(pristineBoxItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(pristineToyItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(genericItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
            Instance.RegisterItemsAsScrap(goldenStatueItems, configEntryForScrapMethod.Value, configEntryForScrapRegistrationList.Value);
        }

    }
}
