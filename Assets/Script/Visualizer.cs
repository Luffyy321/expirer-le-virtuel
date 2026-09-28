using UnityEngine;

// boule temporaire pour visualiser la sortie du BreathDetector
public class Visualizer : MonoBehaviour
{
    public BreathDetector detecteur;

    public float vitesseGonfle = 1f;
    public float vitesseDegonfle = 0.15f;
    public float tailleMin = 0.3f;
    public float tailleMax = 1.5f;

    void Start()
    {
        GetComponent<Renderer>().material.color = Color.blue;
        transform.localScale = new Vector3(tailleMin, tailleMin, tailleMin);
    }

    void Update()
    {
        // on attend la fin du calibrage
        if (!detecteur.calibrationTerminee) return;

        float taille = transform.localScale.x;

        if (detecteur.souffle)
            taille += vitesseGonfle * Time.deltaTime;
        else
            taille -= vitesseDegonfle * Time.deltaTime;

        taille = Mathf.Clamp(taille, tailleMin, tailleMax);
        transform.localScale = new Vector3(taille, taille, taille);
    }
}