using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireTrigger : MonoBehaviour
{
    public ParticleSystem fireEffect;
    public AudioSource fireAudio; 
    public Key triggerKey = Key.Space;
    void Update()
    {
        if (Keyboard.current[triggerKey].wasPressedThisFrame)
        {
            fireEffect.Play();
            if (fireAudio != null && !fireAudio.isPlaying)
                fireAudio.Play();
        }

        if (Keyboard.current[triggerKey].wasReleasedThisFrame)
        {
            fireEffect.Stop();
            if (fireAudio != null)
                fireAudio.Stop();
        }
    }
}