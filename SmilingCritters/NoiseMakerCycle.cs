using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace SmilingCritters
{
    //A near exact duplicate of NoiseMaker that also extends GrabbableObject so I can make scrap that plays audio in cycles.
    public class NoiseMakerCycle : GrabbableObject
    {
        public AudioSource noiseAudio;

        public AudioSource noiseAudioFar;

        [Space(3f)]
        public AudioClip[] noiseSFX;

        public AudioClip[] noiseSFXFar;

        [Space(3f)]
        public float noiseRange;

        public float maxLoudness;

        public float minLoudness;

        public float minPitch;

        public float maxPitch;

        private System.Random noisemakerRandom;

        public int currentSelectedSoundEffect;

        public Animator triggerAnimator;

        public override void Start()
        {
            base.Start();
            noisemakerRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
            currentSelectedSoundEffect = 0;
        }
        
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!(GameNetworkManager.Instance.localPlayerController == null))
            {
                
                int num = currentSelectedSoundEffect;
                float num2 = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
                float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
                noiseAudio.pitch = pitch;
                noiseAudio.PlayOneShot(noiseSFX[num], num2);
                if (noiseAudioFar != null)
                {
                    noiseAudioFar.pitch = pitch;
                    noiseAudioFar.PlayOneShot(noiseSFXFar[num], num2);
                }
                if (triggerAnimator != null)
                {
                    triggerAnimator.SetTrigger("playAnim");
                }
                WalkieTalkie.TransmitOneShotAudio(noiseAudio, noiseSFX[num], num2);
                RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, num2, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
                if (minLoudness >= 0.6f && playerHeldBy != null)
                {
                    playerHeldBy.timeSinceMakingLoudNoise = 0f;
                }
                currentSelectedSoundEffect++;
                if (currentSelectedSoundEffect >= noiseSFX.Length)
                {
                    currentSelectedSoundEffect = 0;
                }
            }
        }
    }
}
