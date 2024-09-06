using UnityEngine;
using LethalConfig.ConfigItems;
using static SmilingCritters.SmilingCritters;
using UnityEngine.AI;

namespace SmilingCritters
{
    /**
     * Handles configuring and registering the smiling critters.
     */
    internal class CreatureConfig
    {

        private static LethalLib.Modules.Levels.LevelTypes chosenCreatureRegistrationMethod;

        private static RarityAddTypes defaultCreatureRegistrationMethod = RarityAddTypes.All;

        private static int bobbyRuinedRarity;
        private static int bubbaRuinedRarity;
        private static int catnapRuinedRarity;
        private static int craftyRuinedRarity;
        private static int dogdayRuinedRarity;
        private static int hoppyRuinedRarity;
        private static int kickinRuinedRarity;
        private static int pickyRuinedRarity;


        internal static void RegisterAllCritters(SmilingCritters Instance)
        {
            string[] critterNames = { "Bobby", "Catnap", "Bubba", "Crafty", "Dogday", "Hoppy", "Kickin", "Picky" };
                createConfigEntries(Instance);
                ConfigureAndRegisterCritter(critterNames[0], bobbyRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[1], bubbaRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[2], catnapRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[3], craftyRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[4], dogdayRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[5], hoppyRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[6], kickinRuinedRarity);
                ConfigureAndRegisterCritter(critterNames[7], pickyRuinedRarity);
        }

        internal static void createConfigEntries(SmilingCritters Instance)
        {
            //Enumerated way to register the smiling critters.
            var configEntryForScrapMethod = Instance.Config.Bind("Creatures", "Registration Method", RarityAddTypes.All
                , "The method to add scrap to the level. \n Default = All \n Vanilla \n Modded \n List (Not yet implemented for creatures, defaults to All)");
            var enumCreatureRegistration = new EnumDropDownConfigItem<RarityAddTypes>(configEntryForScrapMethod);
            switch (configEntryForScrapMethod.Value)
            {
                case RarityAddTypes.All:
                    chosenCreatureRegistrationMethod = LethalLib.Modules.Levels.LevelTypes.All;
                    break;
                case RarityAddTypes.Vanilla:
                    chosenCreatureRegistrationMethod = LethalLib.Modules.Levels.LevelTypes.Vanilla;
                    break;
                case RarityAddTypes.Modded:
                    chosenCreatureRegistrationMethod = LethalLib.Modules.Levels.LevelTypes.Modded;
                    break;
                case RarityAddTypes.List:
                    chosenCreatureRegistrationMethod = LethalLib.Modules.Levels.LevelTypes.All;
                    break;
            }

            var rarityEntryBobbyRuined = Instance.CreateIntSliderConfig("Bobby Bearhug Critter", 10, "Adjust how often you see the enemy Bobby Bearhug as a ruined critter.", 0, 100, "Creatures");
            bobbyRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryBubbaRuined = Instance.CreateIntSliderConfig("Bubba Bubbaphant Critter", 10, "Adjust how often you see the enemy Bubba Bubbaphant as a ruined critter.", 0, 100, "Creatures");
            bubbaRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryCatnapRuined = Instance.CreateIntSliderConfig("Catnap Critter", 10, "Adjust how often you see the enemy Catnap as a ruined critter.", 0, 100, "Creatures");
            catnapRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryCraftyRuined = Instance.CreateIntSliderConfig("Craftycorn Critter", 10, "Adjust how often you see the enemy Craftycorn as a ruined critter.", 0, 100, "Creatures");
            craftyRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryDogdayRuined = Instance.CreateIntSliderConfig("Dogday Critter", 10, "Adjust how often you see the enemy Dogday as a ruined critter.", 0, 100, "Creatures");
            dogdayRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryHoppyRuined = Instance.CreateIntSliderConfig("Hoppy Hopscotch Critter", 10, "Adjust how often you see the enemy Hoppy Hopscotch as a ruined critter.", 0, 100, "Creatures");
            hoppyRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryKickinRuined = Instance.CreateIntSliderConfig("Kickin Chicken Critter", 10, "Adjust how often you see the enemy Kickin Chicken as a ruined critter.", 0, 100, "Creatures");
            kickinRuinedRarity = rarityEntryBobbyRuined.Value;
            var rarityEntryPickyRuined = Instance.CreateIntSliderConfig("Picky Piggy Critter", 10, "Adjust how often you see the enemy Picky Piggy as a ruined critter.", 0, 100, "Creatures");
            pickyRuinedRarity = rarityEntryBobbyRuined.Value;
        }

        private static void ConfigureAndRegisterCritter(string critterName, int rarity)
        {
            //Load the critter
            string enemyTypeName = critterName + "RuinedEnemyType";
            EnemyType critter = SmilingCritters.assetBundle.LoadAsset<EnemyType>(enemyTypeName);
            GameObject critterPrefab = critter.enemyPrefab;

            //Configure the AI
            RuinedCritterAI critterAI = critterPrefab.AddComponent<RuinedCritterAI>();
            critterAI.creatureVoice = critterPrefab.GetComponent<AudioSource>();
            critterAI.creatureSFX = critterPrefab.GetComponent<AudioSource>();
            critterAI.agent = critterPrefab.GetComponent<NavMeshAgent>();
            critterAI.creatureAnimator = critterPrefab.GetComponent<Animator>();
            critterAI.exitVentAnimationTime = 1;
            critterAI.eye = critterPrefab.transform.Find("Eye");
            critterAI.enemyType = critter;
            critterAI.updatePositionThreshold = 0.25f;
            critterAI.syncMovementSpeed = 0.15f;
            critterAI.enemyBehaviourStates = new EnemyBehaviourState[4];
            critterAI.head = FindDeepChild(critterPrefab.transform, "JNT_Head");
            critterAI.enemyHP = 5;
            critterAI.AIIntervalTime = 0.11f;
            //critterAI.enemyType.canDie = false;
            critterAI.eyeComponentReference = critterPrefab.transform.Find(critterName + "Eyes").GetComponent<SkinnedMeshRenderer>();
            critterAI.whiteEyeMaterial = assetBundle.LoadAsset<Material>("RuinedSmallEyeMaterial");
            critterAI.redEyeMaterial = assetBundle.LoadAsset<Material>("RedEyesCritters");
            

            //Set the main script for the collision detection component*****
            string modelContainerName = critterName + "RuinedModelContainer";
            critterPrefab.transform.Find(modelContainerName).GetComponent<EnemyAICollisionDetect>().mainScript = critterAI;

            //Set Audio sources
            critterAI.biteSFX = critterPrefab.transform.Find("BiteSFX").GetComponent<AudioSource>();
            critterAI.creepySFX = critterPrefab.transform.Find("CreepySFX").GetComponent<AudioSource>();
            critterAI.injuredSFX = critterPrefab.transform.Find("InjuredSFX").GetComponent<AudioSource>();
            critterAI.growlSFX = critterPrefab.transform.Find("GrowlSFX").GetComponent<AudioSource>();

            //Add and load aduio clips.
            //Footstep clips
            critterAI.footstepClips = new AudioClip[8];
            critterAI.footstepClips[0] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep1");
            critterAI.footstepClips[1] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep2");
            critterAI.footstepClips[2] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep3");
            critterAI.footstepClips[3] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep4");
            critterAI.footstepClips[4] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep5");
            critterAI.footstepClips[5] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep6");
            critterAI.footstepClips[6] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep7");
            critterAI.footstepClips[7] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep8");

            //Biting clips
            critterAI.biteClips = new AudioClip[7];
            critterAI.biteClips[0] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite1");
            critterAI.biteClips[1] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite2");
            critterAI.biteClips[2] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite3");
            critterAI.biteClips[3] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite4");
            critterAI.biteClips[4] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite5");
            critterAI.biteClips[5] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite6");
            critterAI.biteClips[6] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterBite7");

            //Injured clips
            critterAI.injuredClips = new AudioClip[4];
            critterAI.injuredClips[0] = assetBundle.LoadAsset<AudioClip>("CritterInjured1");
            critterAI.injuredClips[1] = assetBundle.LoadAsset<AudioClip>("CritterInjured2");
            critterAI.injuredClips[2] = assetBundle.LoadAsset<AudioClip>("CritterInjured3");
            critterAI.injuredClips[3] = assetBundle.LoadAsset<AudioClip>("CritterInjured4");

            //Death sound clips
            critterAI.deathClips = new AudioClip[3];
            critterAI.deathClips[0] = assetBundle.LoadAsset<AudioClip>("SmilingCritterDeath1");
            critterAI.deathClips[1] = assetBundle.LoadAsset<AudioClip>("SmilingCritterDeath2");
            critterAI.deathClips[2] = assetBundle.LoadAsset<AudioClip>("SmilingCritterDeath3");

            //Register the critter prefab
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(critterPrefab);

            //Load and register the terminal node and keyword
            string terminalNodeName = critterName + " Critter Node";
            string terminalKeywordName = critterName + " Critter Keyword";
            TerminalNode critterTerminalNode = SmilingCritters.assetBundle.LoadAsset<TerminalNode>(terminalNodeName);
            TerminalKeyword critterTerminalKeyword = SmilingCritters.assetBundle.LoadAsset<TerminalKeyword>(terminalKeywordName);
            if (rarity < 0 || rarity > 100)
            {
                rarity = 10; //Sanity check.
            }

            LethalLib.Modules.Enemies.RegisterEnemy(critter, rarity, chosenCreatureRegistrationMethod, critterTerminalNode, critterTerminalKeyword);
        }

        public static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                Transform result = FindDeepChild(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
