using UnityEngine;
using UnityEngine.InputSystem;

public class FireTrigger : MonoBehaviour
{
    public ParticleSystem fireEffect;
    public AudioSource fireAudio;
    public Transform mouthPoint;
    public float fireRange = 5f;
    public float fireRadius = 0.3f;

    public BreathDetector breathDetector; 

    public bool autoriserToucheDebug = true;
    public Key triggerKey = Key.Space;

    private bool souffleEnCours = false;

    void Update()
    {
        bool souffleActif = (breathDetector != null && breathDetector.souffle)
                             || (autoriserToucheDebug && Keyboard.current[triggerKey].isPressed);

        if (souffleActif && !souffleEnCours)
        {
            souffleEnCours = true;
            fireEffect.Play();
            if (fireAudio != null && !fireAudio.isPlaying)
                fireAudio.Play();
        }

        if (!souffleActif && souffleEnCours)
        {
            souffleEnCours = false;
            fireEffect.Stop();
            if (fireAudio != null)
                fireAudio.Stop();
        }

        if (souffleEnCours)
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