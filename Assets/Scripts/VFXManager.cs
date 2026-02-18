using System.Collections;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [CoolHeader("VFX JUICE!!")]
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem correctAnswerParticles;
    [SerializeField] private ParticleSystem comboParticles;
    [SerializeField] private ParticleSystem heartLossParticles;
    
    [Header("Combo UI")]
    [SerializeField] private GameObject comboText;
    
    [Header("Camera Shake")]
    [SerializeField] private VoltixCamShaker camShaker;
    
    [Header("Combo Settings")]
    [SerializeField] private float comboTimeThreshold = 10f;
    
    private int consecutiveFastSolves = 0;
    private float cardStartTime;

    void Start()
    {
        // Hide combo text initially
        if (comboText != null)
            comboText.SetActive(false);
        
        // Try to find VoltixCamShaker if not assigned
        if (camShaker == null)
            camShaker = FindObjectOfType<VoltixCamShaker>();
    }

    public void StartCardTimer()
    {
        cardStartTime = Time.time;
    }

    public void PlayCorrectAnswerEffects(Transform cardTransform)
    {
        if (cardTransform == null) return;

        float solveTime = Time.time - cardStartTime;
        bool isFastSolve = solveTime <= comboTimeThreshold;
        
        if (isFastSolve)
        {
            consecutiveFastSolves++;
        }
        else
        {
            consecutiveFastSolves = 0;
        }
        
        // Play normal correct answer particles
        if (correctAnswerParticles != null)
        {
            correctAnswerParticles.transform.position = cardTransform.position;
            correctAnswerParticles.Play();
        }
        
        // If combo (2+ fast solves), play extra cool particles
        bool isCombo = consecutiveFastSolves >= 2;
        
        if (isCombo)
        {
            if (comboParticles != null)
            {
                comboParticles.transform.position = cardTransform.position;
                comboParticles.Play();
            }
            
            if (comboText != null)
                comboText.SetActive(true);
            
            // Trigger combo shake
            if (camShaker != null)
                camShaker.ShakeCombo();
        }
        else
        {
            if (comboText != null)
                comboText.SetActive(false);
            
            // Trigger normal shake
            if (camShaker != null)
                camShaker.ShakeNormal();
        }
    }

    public void PlayHeartLossEffect(Transform heartTransform)
    {
        if (heartLossParticles == null || heartTransform == null) return;

        // Instantiate particle system as child of the heart
        ParticleSystem particles = Instantiate(heartLossParticles, heartTransform);
        particles.transform.localPosition = Vector3.zero;
        particles.Play();
        
        // Destroy particle system after it finishes playing
        Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        
        // Optional: Trigger a shake on heart loss
        if (camShaker != null)
            camShaker.ShakeNormal();
    }

    public void ResetCombo()
    {
        consecutiveFastSolves = 0;
        
        if (comboText != null)
            comboText.SetActive(false);
    }

    public int GetComboCount()
    {
        return consecutiveFastSolves;
    }
}