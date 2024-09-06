using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.NetworkManager;

namespace SmilingCritters
{
    public class RuinedCritterAI : EnemyAI
    {
        public AISearchRoutine searchForPlayers;

        private static System.Random rngGenerator;

        private static Vector3 mainEntrancePosition;
        private static Vector3 furthestFromMainEntrancePosition;

        public Transform head;

        bool rotatingHeadTowardsPlayer = true;

        private Vector3 defaultHeadEulerAngles = new Vector3(10.782f, -2.204f, 80.168f);
        private float maxYRotation = 35f;
        private float minYRotation = -35f;
        private float maxZRotation = 60f;
        private float headRotationSpeed = 4f;

        private bool rotatingTowardsTargetPlayer = true;
        public float bodyRotationThreshold = 60f;
        public float bodyRotationSpeed = 4f;
        public Quaternion targetBodyRotation;
        public bool rotatingBody = false;
        public int rotateBodyDirection = 0;
        private float reservedRotationSpeed;

        // Initial and target head positions
        private Vector3 initialHeadPosition = new Vector3(-0.001192911f, 0.00021f, 2.9e-05f);
        private Vector3 targetHeadPosition = new Vector3(-0.001652f, -0.000247f, -7.6e-05f);
        private float headZMaxYRot = 0.0008f;
        private float headZMinYRot= -0.0008f;
        private float defaultHeadZPos = 2.9e-05f;
        public enum Personality
        {
            Hyperaggressive,
            Aggressive,
            Balanced,
        }

        public Personality personality;

        private bool constantAggression = false;

        private float agentMultiplier;

        private float minTimeToGrowlScreech = 7f;

        private float maxTimeToGrowlScreech = 14f;

        private float currentTimeUntilNextGrowlScreech = 14f;

        public bool fleeing = false;

        public bool fleePositionFound = false;

        private float fleeTime;

        public float currentFleeTime = 0;

        public int timesToFlee = 2;

/*        public bool wasHitEarly = false;

        public bool wasHit = false;*/

        private float aggressionTime = 50;

        public float currentAggressionTime = 0;

        public PlayerControllerB targetObservedLast;

        public Vector3 fleeDestination;

        public Vector3 leaveDestination;

        public bool leaveDestinationFound = false;

        public bool leavingLOS = false;

        private float leaveTimeLOS = 6;

        public float currentLeaveTimeLOS = 0;

        public float timeSinceHittingPlayer = 0;


        private enum BehaviorState
        {
            Observing,
            Chasing,
            Retreating,
            Leaving
        }

        private float observationTime = 0;

        public float currentObservationTime = 0;

        public bool atObservationPost = false;

        public AudioSource biteSFX;

        public AudioSource injuredSFX;

        public AudioSource creepySFX;

        public AudioSource growlSFX;

        public AudioClip[] footstepClips;

        public AudioClip[] biteClips;

        public AudioClip[] injuredClips;

        public AudioClip[] creepyClips;

        public AudioClip[] growlClips;

        public AudioClip[] deathClips;

        //Delayed Audio stuff for attacking
        private bool delayingDamage = false;

        private readonly float timeUntilDealingDamage = 0.4f;

        private float currentTimeUntilDealingDamage = 0.0f;

        private PlayerControllerB delayedDamageTarget;

        //Delayed audio attacking.
        private readonly float timeUntilTakingDamageSoundLimit = 2.0f;

        private float currentTimeUntilTakingDamageSoundLimit = 0.0f;

        private bool delayingDamageSoundEffects = false;

        public SkinnedMeshRenderer eyeComponentReference;
        public Material whiteEyeMaterial;
        public Material redEyeMaterial;


        public override void Start()
        {
            base.Start();
            if (rngGenerator == null)
            {
                rngGenerator = new System.Random(StartOfRound.Instance.randomMapSeed + 1338);
            }
            if (mainEntrancePosition == null && furthestFromMainEntrancePosition == null)
            {
                mainEntrancePosition = RoundManager.FindMainEntrancePosition();
                furthestFromMainEntrancePosition = ChooseFarthestNodeFromPosition(mainEntrancePosition).position;
            }
            this.personality = (Personality)rngGenerator.Next(2);
            switch (personality)
            {
                case Personality.Hyperaggressive:
                    agentMultiplier = 2f;
                    agent.angularSpeed = agent.angularSpeed * agentMultiplier;
                    minTimeToGrowlScreech = minTimeToGrowlScreech / agentMultiplier;
                    maxTimeToGrowlScreech = maxTimeToGrowlScreech / agentMultiplier;
                    timesToFlee = 2;
                    aggressionTime = 50f;
                    observationTime = 6;
                    fleeTime = 20;
                    break;
                case Personality.Aggressive:
                    agentMultiplier = 1.3f;
                    agent.angularSpeed = agent.angularSpeed * agentMultiplier;
                    minTimeToGrowlScreech = minTimeToGrowlScreech / agentMultiplier;
                    maxTimeToGrowlScreech = maxTimeToGrowlScreech / agentMultiplier;
                    aggressionTime = 45f;
                    observationTime = 12;
                    fleeTime = 25;
                    break;
                case Personality.Balanced:
                    agentMultiplier = 1f;
                    observationTime = 18;
                    aggressionTime = 45f;
                    fleeTime = 35;
                    break;
            }
            reservedRotationSpeed = agent.angularSpeed;
            if (creatureAnimator.layerCount == 2)
            {
                creatureAnimator.SetLayerWeight(1, 0);
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (StartOfRound.Instance.allPlayersDead || enemyHP < 0)
            {
                return;
            }
            //Change ownership if needed.
            if (!base.IsServer)
            {
                ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            bool atLeastOnePLayerInDungeon = false;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].isInsideFactory)
                {
                    if (searchForPlayers.inProgress)
                    {
                        StopSearch(searchForPlayers);
                    }
                    atLeastOnePLayerInDungeon = true;
                    break;
                }
            }
            if (!atLeastOnePLayerInDungeon && (currentBehaviourStateIndex == 1 || currentBehaviourStateIndex == 0))
            {
                if (!searchForPlayers.inProgress)
                {
                    StartSearch(transform.position, searchForPlayers);
                }
                return;
            }

            float distanceFromTargetPlayer = GetDistanceFromPlayer(targetPlayer);
            switch (currentBehaviourStateIndex)
            {
                case (int)BehaviorState.Observing:
                    if (currentObservationTime >= observationTime/* || wasHitEarly*/)
                    {
                        SwitchFromObservingToFleeing();
                        return;
                    }
                    switch (personality)
                    {
                        case Personality.Hyperaggressive:
                            ChooseStalkTarget();
                            ObserveFromDistance(distanceFromTargetPlayer, 4f);
                            break;
                        case Personality.Aggressive:
                            ChooseStalkTarget();
                            ObserveFromDistance(distanceFromTargetPlayer, 5f);
                            break;
                        case Personality.Balanced:
                            ChooseStalkTarget();
                            ObserveFromDistance(distanceFromTargetPlayer, 5f);
                            break;
                    }
                    break;
                case (int)BehaviorState.Chasing:
                    if ((/*wasHit || */currentAggressionTime > aggressionTime) && timesToFlee > 0)
                    {
                        currentAggressionTime = 0;
                        timesToFlee -= 1;
                        if (timesToFlee <= 0)
                        {
                            SwitchToBehaviourState((int)BehaviorState.Leaving);
                            break;
                        }
                        fleeing = true;
                        SwitchToBehaviourState((int)BehaviorState.Retreating);
                        break;
                    }
                    if (targetPlayer != null && !targetPlayer.isPlayerDead)
                    {
                        SetDestinationToPosition(targetPlayer.transform.position);
                    }
                    else if (atLeastOnePLayerInDungeon)
                    {
                        TargetClosestPlayer(0f, false, 360); //CHANGED FROM GETCLOSEST
                    }
                    break;
                case (int)BehaviorState.Retreating:
                    if (currentFleeTime > fleeTime)
                    {
                        fleeing = false;
                        currentFleeTime = 0;
                        fleePositionFound = false;
                        if (targetObservedLast != null && !targetObservedLast.isPlayerDead)
                        {
                            targetPlayer = targetObservedLast;
                        }
                        else
                        {
                            TargetClosestPlayer(0f, false, 360); //CHANGED FROM GETCLOSEST
                        }
                        moveTowardsDestination = false;
                        movingTowardsTargetPlayer = true;
                        /*wasHit = false;*/
                        CritterSendStringClientRcp("setEyeColorRed");
                        eyeComponentReference.SetMaterial(redEyeMaterial);
                        SwitchToBehaviourState((int)BehaviorState.Chasing);
                        return;
                    }
                    //Sanity Checks Retreating
                    //Eye Color Check
                    if (eyeComponentReference.material != whiteEyeMaterial)
                    {
                        //Debug.Log("Emergency check setting eyes to white.");
                        eyeComponentReference.SetMaterial(whiteEyeMaterial);
                        CritterSendStringClientRcp("setEyeColorWhite");
                    }
                    //Speed Check
                    if (agent.speed < 5)
                    {
                        agent.speed = 6;
                    }
                    //Rotating Set
                    rotatingBody = false;
                    rotatingTowardsTargetPlayer = false;

                    if (targetPlayer != null && !fleePositionFound && !targetPlayer.isPlayerDead)
                    {
                        Vector3 furthestFromCurrentPosition = ChooseFarthestNodeFromPosition(this.transform.position, false).position;
                        fleeDestination = furthestFromCurrentPosition;
                        SetDestinationToPosition(furthestFromCurrentPosition, true);
                        fleePositionFound = true;
                    }
                    else if (!fleePositionFound)
                    {
                        fleeDestination = furthestFromMainEntrancePosition;
                        SetDestinationToPosition(furthestFromMainEntrancePosition);
                        fleePositionFound = true;
                    }
                    else
                    {
                        SetDestinationToPosition(furthestFromMainEntrancePosition, true);
                    }
                    if (Vector3.Distance(transform.position, fleeDestination) < 1)
                    {
                        fleePositionFound = false;
                    }
                    break;
                case (int)BehaviorState.Leaving:
                    if (currentLeaveTimeLOS > leaveTimeLOS)
                    {
                        this.KillEnemy(true);
                    }
                    if (!leaveDestinationFound)
                    {
                        leaveDestination = ChooseFarthestNodeFromPosition(this.transform.position, true).position;
                        leaveDestinationFound = true;
                        SetDestinationToPosition(leaveDestination, true);
                    }
                    for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
                    {
                        if (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(this.transform.position, 360, 100))
                        {
                            currentLeaveTimeLOS = 0;
                            leavingLOS = false;
                        }
                        else
                        {
                            leavingLOS = true;
                        }
                    }
                    if (Vector3.Distance(transform.position, leaveDestination) < 1 && leaveDestinationFound)
                    {
                        leaveDestinationFound = false;
                    }
                    break;
            }
        }

        private void SwitchFromObservingToFleeing()
        {
            fleeing = true;
            atObservationPost = false;
            currentObservationTime = 0;
            rotatingTowardsTargetPlayer = false;
            SetAnimationState("Forward");
            //creatureAnimator.SetTrigger("setForward"); //TODO REPLACE WITH RPC METHOD
            if (creatureAnimator.GetCurrentStateName(0).Equals("Bobby_Ruined_Strafe_Left") || creatureAnimator.GetCurrentStateName(0).Equals("Bobby_Ruined_Strafe_Right"))
            {
                creatureAnimator.Play("setForward", 0); ; //TODO REPLACE WITH RPCANIMATION METHOD 
            }
            creatureAnimator.speed = 2f;
            agent.speed = 6f;
            agent.angularSpeed = 135f;
            SwitchToBehaviourState((int)BehaviorState.Retreating);
            targetObservedLast = targetPlayer;
            CritterSendStringClientRcp("setTransitionObserveToFleeing");
        }

        private void SwitchFromObservingToFleeingEarly()
        {
            fleeing = true;
            atObservationPost = false;
            currentObservationTime = 0;
            rotatingTowardsTargetPlayer = false;
            SetAnimationState("Forward"); //TODO REPLACE WITH RPCANIMATION METHOD 
            creatureAnimator.speed = 2f;
            agent.speed = 6f;
            agent.angularSpeed = 135f;
            SwitchToBehaviourState((int)BehaviorState.Retreating);
            targetObservedLast = targetPlayer;
            CritterSendStringClientRcp("setTransitionObserveToFleeing");
        }

        private void SwitchFromChasingEarly()
        {
            currentAggressionTime = 0;
            timesToFlee -= 1;
            if (timesToFlee <= 0)
            {
                SwitchToBehaviourState((int)BehaviorState.Leaving);
                return;
            }
            eyeComponentReference.SetMaterial(whiteEyeMaterial);
            fleeing = true;
            SwitchToBehaviourState((int)BehaviorState.Retreating);
        }

        private void ObserveFromDistance(float distanceFromPlayer, float targetObservationDistance)
        {
            if (targetPlayer != null)
            {
                if (distanceFromPlayer > targetObservationDistance && !atObservationPost)
                {
                    SetDestinationToPosition(targetPlayer.transform.position, true);
                }
                if (distanceFromPlayer <= targetObservationDistance && !atObservationPost && CheckLineOfSightForPosition(targetPlayer.playerEye.transform.position, 360))
                {
                    agent.velocity = Vector3.zero;
                    destination = this.transform.position;
                    reservedRotationSpeed = agent.angularSpeed;
                    agent.angularSpeed = 0;
                    SetAnimationState("Idling");
                    //creatureAnimator.SetTrigger("setIdle");
                    CritterSendStringClientRcp("setIdleObservation");
                    //TODO REPLACE WITH RPC METHOD LATER
                    atObservationPost = true;
                }
                if (atObservationPost && (distanceFromPlayer > targetObservationDistance * 1.7 || !CheckLineOfSightForPosition(targetPlayer.playerEye.transform.position, 360)))
                {
                    //creatureAnimator.SetTrigger("setForward");
                    //TODO REPLACE WITH RPC METHOD LATER
                    SetAnimationState("Forward");
                    agent.angularSpeed = reservedRotationSpeed;
                    atObservationPost = false;
                    CritterSendStringClientRcp("setForwardObservation");
                }
            }
        }

        private void ChooseStalkTarget()
        {
            PlayerControllerB closestPlayer = GetClosestPlayer();
            if (closestPlayer == targetPlayer || closestPlayer == null || StartOfRound.Instance.livingPlayers == 0)
            {
                return;
            }
            targetPlayer = closestPlayer;
        }

        public override void Update()
        {
            if (isEnemyDead)
            {
                return;
            }
            base.Update();
            RotateTowardsPlayer(targetPlayer);
            if (rotatingTowardsTargetPlayer && atObservationPost)
            {
                if (rotatingBody)
                {
                    switch (rotateBodyDirection)
                    {
                        case 1:
                            if (!creatureAnimator.GetAnimatorStateName(0, true).Contains("Right"))
                            {
                                CritterSendStringClientRcp("setStrafeRight");
                                SetAnimationState("StrafeRight");
                            }
                            break;
                        case -1:
                            if (!creatureAnimator.GetAnimatorStateName(0, true).Contains("Left"))
                            {
                                CritterSendStringClientRcp("setStrafeLeft");
                                SetAnimationState("StrafeLeft");
                            }

                            break;
                    }
                }
                else
                {
                    if (!creatureAnimator.GetAnimatorStateName(0, true).Contains("Idle"))
                    {
                        CritterSendStringClientRcp("setIdle");
                        SetAnimationState("Idling");
                    }
                }
            }
            if (fleeing)
            {
                currentFleeTime += Time.deltaTime;
            }
            if (atObservationPost)
            {
                currentObservationTime += Time.deltaTime;
            }
            if (currentBehaviourStateIndex == (int)BehaviorState.Chasing)
            {
                currentAggressionTime += Time.deltaTime;
            }
            if (leaveDestinationFound && leavingLOS)
            {
                currentLeaveTimeLOS += Time.deltaTime;
            }
            if (timeSinceHittingPlayer > 0)
            {
                timeSinceHittingPlayer -= Time.deltaTime;
            }
            if (currentTimeUntilDealingDamage > 0)
            {
                currentTimeUntilDealingDamage -= Time.deltaTime;
                if (currentTimeUntilDealingDamage <= 0)
                {
                    if (delayedDamageTarget != null)
                    {
                        DealDamageAfterDelay(delayedDamageTarget);
                        delayedDamageTarget = null;
                        delayingDamage = false;
                    }
                    
                }
            }
            if (currentTimeUntilTakingDamageSoundLimit > 0)
            {
                currentTimeUntilTakingDamageSoundLimit -= Time.deltaTime;
                if (currentTimeUntilTakingDamageSoundLimit <= 0)
                {
                    delayingDamageSoundEffects = false;
                }
            }
        }

        public void LateUpdate()
        {
            if (rotatingHeadTowardsPlayer && !isEnemyDead)
            {
                RotateHeadTowardsNearestPlayer();
            }
            
        }
        private float GetDistanceFromPlayer(PlayerControllerB player)
        {
            if (player != null)
            {
                return Vector3.Distance(this.transform.position, player.transform.position);
            }
            return -1;
        }

        private void RotateTowardsPlayer(PlayerControllerB player)
        {
            if (player == null)
            {
                //Don't rotate towards nothing.
                return;
            }
            //Minimum distance to rotate.  Rotations are awkward right on top of the player.
            if (!atObservationPost || GetDistanceFromPlayer(player) < 0.5f || GetDistanceFromPlayer(player) > 15)
            {
                return;
            }

            float angleDiff = GetRotationAngleTowardsTargetPlayer(player);
            float absAngleDiff = Mathf.Abs(angleDiff);
            if (absAngleDiff > bodyRotationThreshold)
            {
                if (angleDiff > 0)
                {
                    rotateBodyDirection = 1;
                }
                else
                {
                    rotateBodyDirection = -1;
                }
                //Determine the target rotation
                Vector3 directionToPlayer = player.transform.position - transform.position;
                directionToPlayer.y = 0; //Ignore vertical difference
                targetBodyRotation = Quaternion.LookRotation(directionToPlayer);

                //We are now going to rotate our body because conditions are met.
                rotatingBody = true;
            }

            if (Math.Abs(transform.rotation.eulerAngles.y - targetBodyRotation.eulerAngles.y) < 5)
            {
                rotatingBody = false;
                rotateBodyDirection = 0;
            }
            if (rotatingBody)
            {
                //Smoothly rotate towards the target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRotation, bodyRotationSpeed * Time.deltaTime);
            }
        }

        private void ResetRotatingVariables()
        {
            rotatingBody = false;
            rotateBodyDirection = 0;

        }
        private float GetRotationAngleTowardsTargetPlayer(PlayerControllerB player)
        {
            if (player != null)
            {
                Vector3 directionToPlayer = player.playerEye.transform.position - eye.transform.position;
                directionToPlayer.y = 0; //Ignore vertical difference

                Vector3 forward = transform.forward;
                forward.y = 0; //Ignore vertical difference

                return Vector3.SignedAngle(forward, directionToPlayer, Vector3.up);
            }
            return 0;
        }

        private PlayerControllerB GetClosestPlayerFixed(Vector3 toThisPosition)
        {
            PlayerControllerB closestPlayer = null;
            float distanceOfClosestPlayerSoFar = 10000f;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead || !StartOfRound.Instance.allPlayerScripts[i].isInsideFactory || StartOfRound.Instance.allPlayerScripts[i].inSpecialInteractAnimation)
                {
                    continue;
                }
                float playerDistanceToPosition = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].transform.position, toThisPosition);
                if (playerDistanceToPosition < distanceOfClosestPlayerSoFar)
                {
                    closestPlayer = StartOfRound.Instance.allPlayerScripts[i];
                    distanceOfClosestPlayerSoFar = playerDistanceToPosition;
                }
            }
            return closestPlayer;
        }

        private void RotateHeadTowardsNearestPlayer()
        {
            PlayerControllerB closestPlayer = MonsterUtility.CheckLineOfSightForClosestPlayerOptimized(this, 360, 25);
            if (closestPlayer == null)
            {
                // Slerp back to the default resting rotation and position
                Quaternion defaultRotation = Quaternion.Euler(defaultHeadEulerAngles);
                head.localRotation = Quaternion.Slerp(head.localRotation, defaultRotation, Time.deltaTime * headRotationSpeed);
                head.localPosition = Vector3.Lerp(head.localPosition, initialHeadPosition, Time.deltaTime * headRotationSpeed);
                return;
            }
            Transform playerEye = closestPlayer.playerEye;
            // Calculate Y Rotation (horizontal)
            Vector3 directionToPlayer = playerEye.position - transform.position;
            float angleToPlayerY = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;
            float baseObjectYRotation = transform.eulerAngles.y;
            float relativeAngleY = Mathf.DeltaAngle(baseObjectYRotation, angleToPlayerY);
            float constrainedYRotation = Mathf.Clamp(relativeAngleY, minYRotation, maxYRotation);

            // Calculate Z Rotation (vertical)
            float distanceXZ = new Vector2(directionToPlayer.x, directionToPlayer.z).magnitude;
            float angleToPlayerZ = Mathf.Atan2(directionToPlayer.y, distanceXZ) * Mathf.Rad2Deg;
            float constrainedZRotation = Mathf.Clamp(angleToPlayerZ, 0f, maxZRotation);

            // Adjust head Z position based on Y rotation
            float yRotationFactor = (constrainedYRotation + maxYRotation) / (2 * maxYRotation);
            float adjustedZPosition = Mathf.Lerp(headZMaxYRot, headZMinYRot, yRotationFactor);
            Vector3 interpolatedPosition = Vector3.Lerp(initialHeadPosition, targetHeadPosition, constrainedZRotation / maxZRotation);
            interpolatedPosition.z = adjustedZPosition;

            // Construct the constrained rotation
            Quaternion targetRotation = Quaternion.Euler(defaultHeadEulerAngles.x, defaultHeadEulerAngles.y - constrainedYRotation, defaultHeadEulerAngles.z + constrainedZRotation);

            // Rotate the head
            head.localRotation = Quaternion.Slerp(head.localRotation, targetRotation, Time.deltaTime * headRotationSpeed);
            head.localPosition = Vector3.Lerp(head.localPosition, interpolatedPosition, Time.deltaTime * headRotationSpeed);
        }


        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            if (isEnemyDead)
            {
                return;
            }
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            enemyHP -= force;
            if (agent.velocity.magnitude > 1)
            {
                //This should reduce by 0.5 effectively
                agent.velocity = agent.velocity.normalized * (agent.velocity.magnitude - 1f);
            }
            if (enemyHP <= 0)
            {
                if (creatureAnimator.layerCount == 2)
                {
                    creatureAnimator.SetLayerWeight(0, 0);
                    creatureAnimator.SetLayerWeight(1, 1);
                    
                }
                CritterSendStringClientRcp("setDying");
                CritterSendStringClientRcp("setEyeColorWhite");
                PlayRandomDeathSound();
                KillEnemyOnOwnerClient();
                return;
            }
            if (currentBehaviourStateIndex == (int)(BehaviorState.Observing))
            {
                if (IsHost || IsServer)
                {
                    SwitchFromObservingToFleeingEarly();
                }
                
            }
            if (currentBehaviourStateIndex == (int)(BehaviorState.Chasing))
            {
                if (IsHost || IsServer)
                {
                    SwitchFromChasingEarly();
                    CritterSendStringClientRcp("setEyeColorWhite");
                }
                
            }
            if (currentTimeUntilTakingDamageSoundLimit <= 0)
            {
                PlayRandomInjuredSound();
                currentTimeUntilTakingDamageSoundLimit = timeUntilTakingDamageSoundLimit;
                delayingDamageSoundEffects = true;
            }
            
        }

        public override void KillEnemy(bool destroy = false)
        {
            base.KillEnemy(destroy);
            creatureAnimator.SetLayerWeight(0, 0);
            creatureAnimator.SetLayerWeight(1, 1);
            creatureSFX.enabled = false;
            creatureAnimator.SetTrigger("KillEnemy");
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (currentBehaviourStateIndex == (int)BehaviorState.Chasing && timeSinceHittingPlayer <= 0f)
            {
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
                if (playerControllerB != null)
                {
                    delayingDamage = true;
                    delayedDamageTarget = playerControllerB;
                    currentTimeUntilDealingDamage = timeUntilDealingDamage;
                    timeSinceHittingPlayer = 1.3f;
                    PlayRandomBiteSound();
                }
            }
        }

        private void DealDamageAfterDelay(PlayerControllerB targetTakingDamage)
        {
            if (targetTakingDamage != null && !targetTakingDamage.isPlayerDead && targetTakingDamage.isInsideFactory)
            {
                targetTakingDamage.DamagePlayer(15, hasDamageSFX: false, callRPC: true, CauseOfDeath.Mauling, 0);
                targetTakingDamage.DropBlood();
                targetTakingDamage.JumpToFearLevel(0.75f);
            }
        }

        public void PlayRandomBiteSound()
        {
            if (biteClips != null && biteSFX != null)
            {
                biteSFX.pitch = 0.9f + (rngGenerator.Next(0, 101) / 1000.0f);
                RoundManager.PlayRandomClip(biteSFX, biteClips, true, 1f);
            }
        }

        public void PlayRandomInjuredSound()
        {
            if (injuredClips != null && injuredSFX != null)
            {
                injuredSFX.pitch = 0.9f + (rngGenerator.Next(0, 101) / 1000.0f);
                RoundManager.PlayRandomClip(injuredSFX, injuredClips, true, 1f);
            }
        }

        public void PlayRandomCreepySound()
        {
            if (creepyClips != null && creepySFX != null)
            {
                RoundManager.PlayRandomClip(creepySFX, creepyClips, true, 1f);
            }
        }

        public void PlayRandomGrowlSound()
        {
            if (growlClips != null && growlSFX != null)
            {
                growlSFX.pitch = 0.9f + (rngGenerator.Next(0, 101)/1000.0f);
                RoundManager.PlayRandomClip(growlSFX, growlClips, true, 1f);
            }
        }

        public void PlayRandomFootStepSound()
        {
            if (footstepClips != null && creatureSFX != null && !isEnemyDead)
            {
                RoundManager.PlayRandomClip(creatureSFX, footstepClips, true, 0.7f);
            }
        }

        public void PlayRandomDeathSound()
        {
            if (deathClips != null && creatureSFX != null)
            {
                RoundManager.PlayRandomClip(creepySFX, deathClips, true, 1.5f);
            }
        }

        private void SetAnimationState(string animationName)
        {
            if (animationName == null)
            {
                return;
            }
            switch (animationName)
            {
                case "Idling":
                    creatureAnimator.SetBool("Idling", true);
                    creatureAnimator.SetBool("Forward", false);
                    creatureAnimator.SetBool("StrafeLeft", false);
                    creatureAnimator.SetBool("StrafeRight", false);
                    break;
                case "Forward":
                    creatureAnimator.SetBool("Idling", false);
                    creatureAnimator.SetBool("Forward", true);
                    creatureAnimator.SetBool("StrafeLeft", false);
                    creatureAnimator.SetBool("StrafeRight", false);
                    break;
                case "StrafeLeft":
                    creatureAnimator.SetBool("Idling", false);
                    creatureAnimator.SetBool("Forward", false);
                    creatureAnimator.SetBool("StrafeLeft", true);
                    creatureAnimator.SetBool("StrafeRight", false);
                    break;
                case "StrafeRight":
                    creatureAnimator.SetBool("Idling", false);
                    creatureAnimator.SetBool("Forward", false);
                    creatureAnimator.SetBool("StrafeLeft", false);
                    creatureAnimator.SetBool("StrafeRight", true);
                    break;
                case "Dying":
                    creatureAnimator.SetBool("Idling", false);
                    creatureAnimator.SetBool("Forward", false);
                    creatureAnimator.SetBool("StrafeLeft", false);
                    creatureAnimator.SetBool("StrafeRight", false);
                    break;
            }
        }

        private void InterpretRcpString(string rcpString)
        {
            if (rcpString.StartsWith("set"))
            {
                if (!IsHost)
                {
                    switch (rcpString)
                    {
                        case "setIdleObservation":
                            //agent.ResetPath();
                            agent.velocity = Vector3.zero;
                            //destination = this.transform.position;
                            SetAnimationState("Idling");
                            atObservationPost = true;
                            break;
                        case "setForwardObservation":
                            SetAnimationState("Forward");
                            atObservationPost = false;
                            break;
                        case "setTransitionObserveToFleeing":
                            fleeing = true;
                            atObservationPost = false;
                            currentObservationTime = 0;
                            rotatingTowardsTargetPlayer = false;
                            SetAnimationState("Forward");
                            if (creatureAnimator.GetCurrentStateName(0).Contains("Strafe_Left") || creatureAnimator.GetCurrentStateName(0).Contains("Strafe_Right"))
                            {
                                SetAnimationState("Forward");
                            }
                            creatureAnimator.speed *= 2f;
                            agent.speed = 6f;
                            targetObservedLast = targetPlayer;
                            break;
                        case "setInitialAgentSpeedValue:":
                            break;
                        case "setStrafeLeft":
                            SetAnimationState("StrafeLeft");
                            rotatingBody = true;
                            break;
                        case "setStrafeRight":
                            SetAnimationState("StrafeRight");
                            rotatingBody = true;
                            break;
                        case "setIdle":
                            SetAnimationState("Idling");
                            rotatingBody = false;
                            break;
                        case "setEyeColorWhite":
                            eyeComponentReference.SetMaterial(whiteEyeMaterial);
                            break;
                        case "setEyeColorRed":
                            eyeComponentReference.SetMaterial(redEyeMaterial);
                            break;
                        case "setDying":
                            SetAnimationState("Dying");
                            creatureAnimator.SetLayerWeight(0, 0);
                            break;
                    }
                }
            }
        }

        [ClientRpc]
        private void CritterSendStringClientRcp(string informationString)
        {
            NetworkManager networkManager = ((NetworkBehaviour)this).NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if ((int)__rpc_exec_stage != 2 && ((networkManager.IsServer || networkManager.IsHost || networkManager.IsClient)))
            {
                ClientRpcParams rpcParams = default(ClientRpcParams);
                FastBufferWriter bufferWriter = __beginSendClientRpc(2947718646u, rpcParams, 0);
                bool flag = informationString != null;
                bufferWriter.WriteValueSafe(flag, default);
                if (flag)
                {
                    bufferWriter.WriteValueSafe(informationString, false);
                }
                __endSendClientRpc(ref bufferWriter, 2947718646u, rpcParams, 0);
            }
            if ((int)__rpc_exec_stage == 2 && (networkManager.IsClient || networkManager.IsHost))
            {
                InterpretRcpString(informationString);
            }
        }

        //Send String information
        private static void __rpc_handler_sendcritter_2947718646(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            NetworkManager networkManager = target.NetworkManager;
            if (networkManager != null && networkManager.IsListening)
            {
                bool flag = default(bool);
                reader.ReadValueSafe(out flag, default);
                string valueAsString = null;
                if (flag)
                {
                    reader.ReadValueSafe(out valueAsString, false);
                }
                target.__rpc_exec_stage = (__RpcExecStage)2;
                ((RuinedCritterAI)target).CritterSendStringClientRcp(valueAsString);
                target.__rpc_exec_stage = (__RpcExecStage)0;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        internal static void InitializeRPCS_Critters()
        {
            __rpc_func_table.Add(2947718646, new RpcReceiveHandler(__rpc_handler_sendcritter_2947718646)); //SendStringClient Host -> Client
        }
    }
}
