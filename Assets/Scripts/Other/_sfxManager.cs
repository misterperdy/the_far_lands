using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _sfxManager : MonoBehaviour
{
    public AudioSource sfxAudioSource;//asign in inspector
    public AudioSource footstepAudioSource;

    [Header("Sounds")]
    public AudioClip[] blockSounds;
    public AudioClip enterWater;
    public AudioClip exitWater;
    public AudioClip dingShort;
    public AudioClip dingLong;
    public AudioClip death;

    public void PlayBlockSound() {
        if(blockSounds.Length > 0) {
            //pick a sound from array and play it

            int randomIndex = Random.Range(0, blockSounds.Length);

            AudioClip clip = blockSounds[randomIndex];

            if(sfxAudioSource != null && clip != null) {
                sfxAudioSource.PlayOneShot(clip);
            }
        }
    }

    public void PlayDingShort() {
        if (sfxAudioSource != null && dingShort != null) {
            sfxAudioSource.PlayOneShot(dingShort);
        }
    }

    public void PlayDingLong() {
        if (sfxAudioSource != null && dingLong != null) {
            sfxAudioSource.PlayOneShot(dingLong);
        }
    }

    public void PlayDeath() {
        if (sfxAudioSource != null && death != null) {
            sfxAudioSource.PlayOneShot(death);
        }
    }

    public void PlayEnterWater() {
        if (sfxAudioSource != null && enterWater != null) {
            sfxAudioSource.PlayOneShot(enterWater);
        }
    }

    public void PlayExitWater() {
        if (sfxAudioSource != null && exitWater != null) {
            sfxAudioSource.PlayOneShot(exitWater);
        }
    }

    public void PlayFootsteps() {
        
        if (footstepAudioSource != null) {
            if (!footstepAudioSource.isPlaying) {
                footstepAudioSource.Play();
            }
            
        }
    }

    public void StopFootsteps() {
        if (footstepAudioSource != null) {
            if (footstepAudioSource.isPlaying) {
                footstepAudioSource.Stop();
            }

        }
    }
}
