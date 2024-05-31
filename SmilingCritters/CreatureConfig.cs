using LethalPlaytime;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AI;
using UnityEngine;

namespace SmilingCritters
{
    internal class CreatureConfig
    {

        private static LethalLib.Modules.Levels.LevelTypes registrationMethod = LethalLib.Modules.Levels.LevelTypes.All;
        internal static void RegisterAllCritters()
        {
            string[] critterNames = { "Bobby", "Catnap", "Bubba", "Crafty", "Dogday", "Hoppy", "Kickin", "Picky" };

            foreach (string critterName in critterNames)
            {
                ConfigureAndRegisterCritter(critterName);
            }
        }

        private static void ConfigureAndRegisterCritter(string critterName)
        {
            // Load the EnemyType
            string enemyTypeName = critterName + "RuinedEnemyType";
            EnemyType critter = SmilingCritters.assetBundle.LoadAsset<EnemyType>(enemyTypeName);
            GameObject critterPrefab = critter.enemyPrefab;

            // Configure the AI
            RuinedCritterAI critterAI = critterPrefab.AddComponent<RuinedCritterAI>();
            critterAI.creatureVoice = critterPrefab.GetComponent<AudioSource>();
            critterAI.creatureSFX = critterPrefab.GetComponent<AudioSource>();
            critterAI.agent = critterPrefab.GetComponent<NavMeshAgent>();
            critterAI.creatureAnimator = critterPrefab.GetComponent<Animator>();
            critterAI.exitVentAnimationTime = 1;
            critterAI.eye = critterPrefab.transform.Find("Eye");
            critterAI.enemyType = critter;
            critterAI.updatePositionThreshold = 0.05f;
            critterAI.syncMovementSpeed = 0.15f;
            critterAI.enemyBehaviourStates = new EnemyBehaviourState[4];
            critterAI.head = FindDeepChild(critterPrefab.transform, "JNT_Head");
            critterAI.enemyHP = 999999999;
            critterAI.AIIntervalTime = 0.10f;

            // Set the main script for the collision detection component
            string modelContainerName = critterName + "RuinedModelContainer";
            critterPrefab.transform.Find(modelContainerName).GetComponent<EnemyAICollisionDetect>().mainScript = critterAI;

            // Register the critter prefab
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(critterPrefab);

            //Add and load aduio clips.
            critterAI.footstepClips = new AudioClip[8];
            critterAI.footstepClips[0] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep1");
            critterAI.footstepClips[1] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep2");
            critterAI.footstepClips[2] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep3");
            critterAI.footstepClips[3] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep4");
            critterAI.footstepClips[4] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep5");
            critterAI.footstepClips[5] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep6");
            critterAI.footstepClips[6] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep7");
            critterAI.footstepClips[7] = SmilingCritters.assetBundle.LoadAsset<AudioClip>("CritterFootstep8");

            // Load and register the terminal node and keyword
            string terminalNodeName = critterName + " Critter Node";
            string terminalKeywordName = critterName + " Critter Keyword";
            TerminalNode critterTerminalNode = SmilingCritters.assetBundle.LoadAsset<TerminalNode>(terminalNodeName);
            TerminalKeyword critterTerminalKeyword = SmilingCritters.assetBundle.LoadAsset<TerminalKeyword>(terminalKeywordName);
            LethalLib.Modules.Enemies.RegisterEnemy(critter, 100, registrationMethod, critterTerminalNode, critterTerminalKeyword);
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
