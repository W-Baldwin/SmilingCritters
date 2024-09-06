using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace SmilingCritters
{
    internal static class MonsterUtility
    {
        public static PlayerControllerB CheckLineOfSightForClosestPlayerOptimized(EnemyAI enemy, float width = 360, int range = 60, int proximityAwareness = -1, float bufferDistance = 0f)
        {
            float num = 1000f;
            float num2 = 1000f;
            int num3 = -1;
            PlayerControllerB returnValue = null;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB checkingPlayer = StartOfRound.Instance.allPlayerScripts[i];
                if (checkingPlayer == null || checkingPlayer.isPlayerDead || !checkingPlayer.isInsideFactory)
                {
                    continue;
                }
                Vector3 position = checkingPlayer.gameplayCamera.transform.position;
                if (!Physics.Linecast(enemy.eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    Vector3 to = position - enemy.eye.position;
                    num = Vector3.Distance(enemy.eye.position, position);
                    if ((Vector3.Angle(enemy.eye.forward, to) < width || (proximityAwareness != -1 && num < (float)proximityAwareness)) && num < num2)
                    {
                        num2 = num;
                        num3 = i;
                    }
                }
            }
            if (enemy.targetPlayer != null && num3 != -1 && enemy.targetPlayer != StartOfRound.Instance.allPlayerScripts[num3] && bufferDistance > 0f && Mathf.Abs(num2 - Vector3.Distance(enemy.transform.position, enemy.targetPlayer.transform.position)) < bufferDistance)
            {
                return null;
            }
            if (num3 < 0)
            {
                return null;
            }
            enemy.mostOptimalDistance = num2;
            if (num3 >= 0)
            {
                returnValue = StartOfRound.Instance.allPlayerScripts[num3];
            }
            return returnValue;
        }
    }
}
