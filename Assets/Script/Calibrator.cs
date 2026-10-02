using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Calibrator : MonoBehaviour
{
    public BreathDetector detector; // a glisser dans l'inspector
    public float dureePhase = 3f; // combien de temps dure chaque phase

    public enum Phase { Intro, Silence, Voix, Souffle, Termine }
    public Phase phaseActuelle = Phase.Intro;
    public float tempsPhase = 0f;
    public bool enPause = true; // vrai tant que l'utilisateur n'a pas clique sur "Je suis pret"

    List<float> volumesSilence = new List<float>();
    List<float> volumesSouffle = new List<float>();
    List<float> volumesVoix = new List<float>();
    List<float> freqsSouffle = new List<float>();
    List<float> freqsVoix = new List<float>();

    void Start()
    {
        // si on a deja calibre une fois, on reprend les seuils sauvegardes
        // (l'ecran d'intro s'affiche quand meme, le calibrage se relance avec le bouton)
        if (PlayerPrefs.HasKey("seuilFreqSouffle"))
        {
            detector.seuilVolumeHaut = PlayerPrefs.GetFloat("seuilVolumeHaut");
            detector.seuilVolumeBas = PlayerPrefs.GetFloat("seuilVolumeBas");
            detector.seuilFreqSouffle = PlayerPrefs.GetFloat("seuilFreqSouffle");
            detector.calibrationTerminee = true;
            Debug.Log("calibrage : seuils sauvegardes recuperes");
        }

        phaseActuelle = Phase.Intro;
    }

    void Update()
    {
        // touche C pour relancer le calibrage
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            LancerCalibrage();
        }

        // si on est sur l'intro, si c'est fini ou si on a pas de detector on fait rien
        if (phaseActuelle == Phase.Intro || phaseActuelle == Phase.Termine) return;
        if (detector == null) return;

        // le detector lit le micro pour nous, si pas assez de data on attend
        if (!detector.RafraichirMesures()) return;

        // on attend que l'utilisateur clique sur "Je suis pret"
        if (enPause) return;

        tempsPhase += Time.deltaTime;

        // selon la phase actuelle on stocke pas les memes trucs
        if (phaseActuelle == Phase.Silence)
        {
            volumesSilence.Add(detector.volumeBrut);
        }
        else if (phaseActuelle == Phase.Voix)
        {
            volumesVoix.Add(detector.volumeBrut);

            // on garde la frequence seulement si ca depasse clairement le bruit de fond
            // sinon ca fausse le calcul avec des mesures de silence
            if (detector.volumeBrut > MoyenneListe(volumesSilence) * 2f)
            {
                freqsVoix.Add(detector.freqPic);
            }
        }
        else if (phaseActuelle == Phase.Souffle)
        {
            volumesSouffle.Add(detector.volumeBrut);

            if (detector.volumeBrut > MoyenneListe(volumesSilence) * 2f)
            {
                freqsSouffle.Add(detector.freqPic);
            }
        }

        // temps ecoule, on passe a la phase suivante
        if (tempsPhase >= dureePhase)
        {
            tempsPhase = 0f;

            if (phaseActuelle == Phase.Silence)
            {
                phaseActuelle = Phase.Voix;
                enPause = true;
            }
            else if (phaseActuelle == Phase.Voix)
            {
                phaseActuelle = Phase.Souffle;
                enPause = true;
            }
            else if (phaseActuelle == Phase.Souffle)
            {
                AppliquerSeuils();
                phaseActuelle = Phase.Termine;
            }
        }
    }

    // appele par le bouton "Je suis pret"
    public void Pret()
    {
        enPause = false;
        tempsPhase = 0f;
    }

    // remet tout a zero et recommence depuis la phase silence
    // appele par les boutons "Commencer le calibrage" et "Recalibrer"
    public void LancerCalibrage()
    {
        volumesSilence.Clear();
        volumesSouffle.Clear();
        volumesVoix.Clear();
        freqsSouffle.Clear();
        freqsVoix.Clear();

        tempsPhase = 0f;
        phaseActuelle = Phase.Silence;
        enPause = true;
        detector.calibrationTerminee = false;

        Debug.Log("calibrage : lance");
    }

    // calcule les seuils a partir de ce qu'on a mesure et les envoie au detector
    void AppliquerSeuils()
    {
        float silenceMoy = MoyenneListe(volumesSilence);
        float souffleMoy = MoyenneListe(volumesSouffle);
        float voixMoy = MoyenneListe(volumesVoix);

        // seuil de volume : entre le silence et le plus bas des deux autres
        float basVolume = Mathf.Min(souffleMoy, voixMoy);
        float seuilHaut = Mathf.Lerp(silenceMoy, basVolume, 0.5f);
        float seuilBas = Mathf.Lerp(silenceMoy, seuilHaut, 0.5f);

        // seuil de frequence : entre le pic "quasi max" du souffle et le pic "quasi min" de la voix
        // (percentile plutot que vrai min/max, pour ignorer une frame parasite isolee)
        float freqSouffleMax = MaxRobuste(freqsSouffle);
        float freqVoixMin = MinRobuste(freqsVoix);
        float seuilFreq = Mathf.Lerp(freqSouffleMax, freqVoixMin, 0.5f);

        detector.seuilVolumeHaut = seuilHaut;
        detector.seuilVolumeBas = seuilBas;
        detector.seuilFreqSouffle = seuilFreq;
        detector.calibrationTerminee = true;

        // on sauvegarde pour ne pas avoir a recalibrer au prochain lancement
        PlayerPrefs.SetFloat("seuilVolumeHaut", seuilHaut);
        PlayerPrefs.SetFloat("seuilVolumeBas", seuilBas);
        PlayerPrefs.SetFloat("seuilFreqSouffle", seuilFreq);
        PlayerPrefs.Save();

        Debug.Log("calibrage fini - volumeHaut : " + seuilHaut
            + " - volumeBas : " + seuilBas
            + " - freqSouffle : " + seuilFreq
            + " (souffleMax : " + freqSouffleMax + " - voixMin : " + freqVoixMin + ")");
    }

    float MoyenneListe(List<float> liste)
    {
        if (liste.Count == 0) return 0f;

        float somme = 0f;
        foreach (float v in liste)
        {
            somme += v;
        }
        return somme / liste.Count;
    }

    // renvoie une valeur proche du maximum, mais ignore les 10% de mesures les plus hautes
    // (evite qu'une seule frame parasite fausse tout le calcul)
    float MaxRobuste(List<float> liste, float pourcentageIgnore = 0.1f)
    {
        if (liste.Count == 0) return 0f;

        List<float> triee = new List<float>(liste);
        triee.Sort();

        int index = Mathf.Clamp(Mathf.FloorToInt(triee.Count * (1f - pourcentageIgnore)), 0, triee.Count - 1);
        return triee[index];
    }

    // meme principe, mais pour le minimum
    float MinRobuste(List<float> liste, float pourcentageIgnore = 0.1f)
    {
        if (liste.Count == 0) return 0f;

        List<float> triee = new List<float>(liste);
        triee.Sort();

        int index = Mathf.Clamp(Mathf.FloorToInt(triee.Count * pourcentageIgnore), 0, triee.Count - 1);
        return triee[index];
    }
}