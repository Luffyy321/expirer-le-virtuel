using UnityEngine;

public class TorchController : MonoBehaviour
{
    public ParticleSystem flameEffect;
    public Light torchLight;
    private bool isLit = false;

    public void Ignite()
    {
        if (isLit) return; // évite de relancer si déjà allumée

        isLit = true;
        flameEffect.Play();
        torchLight.enabled = true;
    }
}