using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CalibrationUI : MonoBehaviour
{
    public Calibrator calibrator;
    public BreathDetector detector;

    // les ecrans (enfants de Screens)
    public GameObject screenIntro;
    public GameObject screenAttente;
    public GameObject screenSilence;
    public GameObject screenLecture;
    public GameObject screenSouffle;
    public GameObject screenTermine;

    // en-tete
    public TMP_Text headerLabel;
    public TMP_Text headerCount;

    // textes qui changent
    public TMP_Text titreAttente;
    public TMP_Text countdownSilence;
    public TMP_Text countdownSouffle;
    public TMP_Text statusText;

    // barres des jauges (les GaugeFill)
    public Image fillSilence;
    public Image fillLecture;
    public Image fillSouffle;

    // titres des etapes, affiches aussi sur l'ecran d'attente
    public string titreSilence = "Rester silencieux";
    public string titreLecture = "Lire à voix haute";
    public string titreSouffle = "Souffler dans le micro";

    // volume qui remplit la jauge en entier, a ajuster selon le micro
    public float volumeMax = 0.05f;

    public Color couleurAccent = new Color(0.949f, 0.694f, 0.204f);
    public Color couleurGris = new Color(0.655f, 0.639f, 0.604f);

    // nom de la scene a charger avec "Lancer l'experience"
    public string sceneExperience;

    void Update()
    {
        Calibrator.Phase phase = calibrator.phaseActuelle;

        if (phase == Calibrator.Phase.Intro)
        {
            AfficherEcran(screenIntro);
            headerLabel.text = "INTRODUCTION";
            headerCount.text = "";
            return;
        }

        headerLabel.text = "CALIBRAGE MICRO";

        if (phase == Calibrator.Phase.Termine)
        {
            AfficherEcran(screenTermine);
            headerCount.text = "OK";

            if (detector.souffle)
            {
                statusText.text = "SOUFFLE DÉTECTÉ";
                statusText.color = couleurAccent;
            }
            else
            {
                statusText.text = "EN ATTENTE";
                statusText.color = couleurGris;
            }
            return;
        }

        // numero et titre de l'etape en cours
        string titre = titreSilence;
        if (phase == Calibrator.Phase.Silence)
        {
            headerCount.text = "1 / 3";
            titre = titreSilence;
        }
        else if (phase == Calibrator.Phase.Voix)
        {
            headerCount.text = "2 / 3";
            titre = titreLecture;
        }
        else if (phase == Calibrator.Phase.Souffle)
        {
            headerCount.text = "3 / 3";
            titre = titreSouffle;
        }

        // avant le clic sur "Je suis pret"
        if (calibrator.enPause)
        {
            AfficherEcran(screenAttente);
            titreAttente.text = titre;
            return;
        }

        // pendant la mesure
        float niveau = Mathf.Clamp01(detector.volumeLisse / volumeMax);
        int restant = Mathf.Max(1, Mathf.CeilToInt(calibrator.dureePhase - calibrator.tempsPhase));
        string countdown = restant.ToString("00") + "<size=20><color=#A7A39A> s</color></size>";

        if (phase == Calibrator.Phase.Silence)
        {
            AfficherEcran(screenSilence);
            countdownSilence.text = countdown;
            fillSilence.fillAmount = niveau;
        }
        else if (phase == Calibrator.Phase.Voix)
        {
            AfficherEcran(screenLecture);
            fillLecture.fillAmount = niveau;
        }
        else if (phase == Calibrator.Phase.Souffle)
        {
            AfficherEcran(screenSouffle);
            countdownSouffle.text = countdown;
            fillSouffle.fillAmount = niveau;
        }
    }

    // affiche seulement l'ecran demande
    // SetActive ne fait rien si l'etat ne change pas, donc on ne desactive pas l'ecran affiche a chaque frame
    void AfficherEcran(GameObject ecran)
    {
        screenIntro.SetActive(ecran == screenIntro);
        screenAttente.SetActive(ecran == screenAttente);
        screenSilence.SetActive(ecran == screenSilence);
        screenLecture.SetActive(ecran == screenLecture);
        screenSouffle.SetActive(ecran == screenSouffle);
        screenTermine.SetActive(ecran == screenTermine);
    }

    // a relier aux boutons dans l'inspector (On Click)

    public void Commencer()
    {
        calibrator.LancerCalibrage();
    }

    public void Pret()
    {
        calibrator.Pret();
    }

    public void Recalibrer()
    {
        calibrator.LancerCalibrage();
    }

    public void LancerExperience()
    {
        SceneManager.LoadScene(sceneExperience);
    }
}