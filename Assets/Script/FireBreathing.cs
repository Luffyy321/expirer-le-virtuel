using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireTrigger : MonoBehaviour
{
    public ParticleSystem fireEffect;
    public AudioSource fireAudio;
    public Key triggerKey = Key.Space;
    public Transform mouthPoint;
    public float fireRange = 5f;
    public float fireRadius = 0.3f;


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

        if (Keyboard.current[triggerKey].isPressed)
        {
            CheckTorchHit();
        }
    }

    void CheckTorchHit()
    {
        Transform origin = mouthPoint != null ? mouthPoint : transform;

        Debug.DrawRay(origin.position, origin.forward * fireRange, Color.red);

        if (Physics.SphereCast(origin.position, fireRadius, origin.forward, out RaycastHit hit, fireRange))
        {
            if (hit.collider.CompareTag("Torch"))
            {
                hit.collider.GetComponent<TorchController>().Ignite();
            }
        }
    }
}