using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SmilingCritters
{
    public class CymbalClashBunzoBunny : MonoBehaviour
    {
        public AudioClip soundClip;  // Assign this in the Unity Inspector
        public float noiseRange;     // Range of the noise
        public float loudness;       // Loudness of the noise
        public float minLoudness;
        public float maxLoudness;
        public float minPitch;
        public float maxPitch;

        public AudioSource audioSource;
        private PlayerControllerB playerHeldBy;
        private bool isInElevator;
        private AnimatedItem animatedItemReference;

        public void Start()
        {
            audioSource = GetComponent<AudioSource>();
            animatedItemReference = GetComponent<AnimatedItem>();
            noiseRange = 35;
            loudness = 1.0f;
            minLoudness = 0.9f;
            maxLoudness = 1.0f;
            minPitch = 0.94f;
            maxPitch = 1.0f;
        }

        public void CymbalClash()
        {
            if (audioSource != null && soundClip != null)
            {
                //Update PlayerControllerB and isInElevator
                if (animatedItemReference != null)
                {
                    playerHeldBy = animatedItemReference.playerHeldBy;
                    isInElevator = animatedItemReference.isInElevator;
                }

                //randomize volume and pitch
                float volume = UnityEngine.Random.Range(minLoudness, maxLoudness);
                float pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                audioSource.pitch = pitch;

                //play the sound
                audioSource.PlayOneShot(soundClip, volume);

                //make the noise play for all and over walkie-talkies
                WalkieTalkie.TransmitOneShotAudio(audioSource, soundClip, volume);
                RoundManager.Instance.PlayAudibleNoise(transform.position, noiseRange, volume, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);

                //enemies can hear loud items
                if (minLoudness >= 0.6f && playerHeldBy != null)
                {
                    playerHeldBy.timeSinceMakingLoudNoise = 0f;
                }
            }
        }
    }
}
