using System;
using System.Numerics;
using UnityEngine;

public class BreathDetector : MonoBehaviour
{
    string micro;
    AudioClip clip;

    const int sampleRate = 16000;
    const int fenetre = 512; // taille de la fenetre, doit rester une puissance de 2 pour la FFT

    float[] buffer;
    Complex[] spectre;

    [SerializeField] float lissage = 15f;
    [SerializeField] float freqMax = 2000f; // on cherche le pic seulement en dessous de ca

    // seuils, reglables a la main ou ecrases par le Calibrator
    public float seuilVolumeHaut = 0.018f;
    public float seuilVolumeBas  = 0.010f;
    public float seuilFreqSouffle = 120f;

    // le Calibrator met ca a vrai une fois qu'il a fini de regler les seuils
    public bool calibrationTerminee = false;

    // dernieres mesures, lisibles par n'importe quel autre script (comme le Calibrator)
    public float volumeBrut;
    public float volumeLisse;
    public float freqPic;

    // vrai quand on detecte un souffle, lisible par les autres scripts
    public bool souffle = false;

    // un souffle doit durer au moins ce temps (en secondes) pour compter
    // ca ignore les bruits courts comme un coup sur la table ou une consonne
    public float dureeMinSouffle = 0.2f;

    bool actif;
    float tempsSouffle = 0f; // depuis combien de temps on entend un souffle sans interruption

    void Start()
    {
        buffer = new float[fenetre];
        spectre = new Complex[fenetre];

        if (Microphone.devices.Length == 0)
        {
            Debug.Log("pas de micro trouvé");
            return;
        }

        micro = Microphone.devices[0];
        Debug.Log("micro choisi : " + micro);

        clip = Microphone.Start(micro, true, 1, sampleRate);
    }

    void Update()
    {
        if (!RafraichirMesures()) return;

        if (!calibrationTerminee)
        {
            souffle = false;
            tempsSouffle = 0f;
            return; // le Calibrator gere tout tant que ce n'est pas fini
        }

        if (!actif && volumeLisse > seuilVolumeHaut) actif = true;
        else if (actif && volumeLisse < seuilVolumeBas) actif = false;

        // on compte depuis combien de temps le son ressemble a un souffle
        if (actif && freqPic < seuilFreqSouffle)
            tempsSouffle += Time.deltaTime;
        else
            tempsSouffle = 0f;

        // on valide le souffle seulement s'il dure assez longtemps
        souffle = tempsSouffle >= dureeMinSouffle;

        if (!actif)
        {
            Debug.Log("silence - volume : " + volumeLisse);
            return;
        }

        if (freqPic < seuilFreqSouffle)
            Debug.Log("souffle - volume : " + volumeLisse + " - pic vers " + freqPic + " Hz");
        else
            Debug.Log("voix - volume : " + volumeLisse + " - pic vers " + freqPic + " Hz");
    }

    // lit le micro et met a jour volumeBrut, volumeLisse et freqPic
    // renvoie false si pas encore assez de donnees
    public bool RafraichirMesures()
    {
        if (clip == null) return false;

        int pos = Microphone.GetPosition(micro);
        if (pos < fenetre) return false;

        clip.GetData(buffer, pos - fenetre);

        float somme = 0f;
        for (int i = 0; i < fenetre; i++)
            somme += buffer[i] * buffer[i];
        volumeBrut = Mathf.Sqrt(somme / fenetre);

        volumeLisse = Mathf.Lerp(volumeLisse, volumeBrut, 1f - Mathf.Exp(-lissage * Time.deltaTime));

        freqPic = CalculerFreqPic();

        return true;
    }

    float CalculerFreqPic()
    {
        for (int i = 0; i < fenetre; i++)
            spectre[i] = new Complex(buffer[i], 0);

        FFT(spectre);

        float resolution = (float)sampleRate / fenetre;
        int indexLimite = Mathf.FloorToInt(freqMax / resolution);

        float maxValeur = 0f;
        int indexPic = 1;

        for (int i = 1; i < indexLimite; i++)
        {
            float magnitude = (float)spectre[i].Magnitude;
            if (magnitude > maxValeur)
            {
                maxValeur = magnitude;
                indexPic = i;
            }
        }

        return indexPic * resolution;
    }

    void FFT(Complex[] donnees)
    {
        int n = donnees.Length;
        if (n <= 1) return;

        Complex[] pairs = new Complex[n / 2];
        Complex[] impairs = new Complex[n / 2];
        for (int i = 0; i < n / 2; i++)
        {
            pairs[i] = donnees[i * 2];
            impairs[i] = donnees[i * 2 + 1];
        }

        FFT(pairs);
        FFT(impairs);

        for (int k = 0; k < n / 2; k++)
        {
            Complex t = Complex.FromPolarCoordinates(1.0, -2 * Math.PI * k / n) * impairs[k];
            donnees[k] = pairs[k] + t;
            donnees[k + n / 2] = pairs[k] - t;
        }
    }
}