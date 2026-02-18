using UnityEngine;
using TMPro;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

public class ProgressionTracker : MonoBehaviour
{
    [CoolHeader("PROGRESSION SYSTEM")]
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI auraText;
    [SerializeField] private TextMeshProUGUI cardsFailedText;
    [SerializeField] private TextMeshProUGUI roundsLostText;

    [Header("XP Settings")]
    [SerializeField] private int fastXP = 15;
    [SerializeField] private int quickXP = 10;
    [SerializeField] private int normalXP = 5;

    [Header("Combo Bonus")]
    [SerializeField] private int comboXPBonus = 5;

    [Header("Animation Settings")]
    [SerializeField] private float xpGainAnimDuration = 0.5f;
    [SerializeField] private Color xpGainColor = Color.yellow;
    [SerializeField] private Color xpLossColor = Color.red;
    [SerializeField] private Color auraGainColor = Color.cyan;
    [SerializeField] private Color auraLossColor = Color.red;

    // Stats
    private int totalXP = 0;
    private int totalAura = 0;
    private int cardsFailed = 0;
    private int roundsLost = 0;

    // Mid-round quit protection
    // We only commit XP/Aura changes to PlayerPrefs at round boundaries (win/lose).
    // During a round, changes are tracked in memory only via these snapshots.
    private int xpSnapshot = 0;
    private int auraSnapshot = 0;
    private bool isRoundInProgress = false;

    // References
    private VFXManager vfxManager;
    private Color originalXPColor;
    private Color originalAuraColor;

    // PlayerPrefs keys
    private const string KEY_XP           = "TotalXP";
    private const string KEY_AURA         = "TotalAura";
    private const string KEY_CARDS_FAILED = "CardsFailed";
    private const string KEY_ROUNDS_LOST  = "RoundsLost";
    private const string KEY_CHECKSUM     = "ProgressChecksum";

    void Start()
    {
        vfxManager = FindObjectOfType<VFXManager>();
        if (vfxManager == null)
            Debug.LogWarning("ProgressionTracker: VFXManager not found!");

        if (xpText != null)  originalXPColor   = xpText.color;
        if (auraText != null) originalAuraColor = auraText.color;

        LoadAndVerifyProgress();
        UpdateAllUI();
    }

    // ─────────────────────────────────────────────
    //  ROUND LIFECYCLE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager at the start of every round.
    /// Snapshots current XP/Aura so we can revert if player quits mid-round.
    /// </summary>
    public void OnRoundStarted()
    {
        xpSnapshot   = totalXP;
        auraSnapshot = totalAura;
        isRoundInProgress = true;
        Debug.Log($"[Progression] Round started. Snapshot — XP:{xpSnapshot} Aura:{auraSnapshot}");
    }

    public void OnRoundWon()
    {
        isRoundInProgress = false;
        AddAura(100);
        CommitProgress(); // Safe to save — round completed cleanly
        Debug.Log("[Progression] Round won — progress committed.");
    }

    public void OnRoundLost()
    {
        isRoundInProgress = false;
        roundsLost++;
        RemoveXP(50);
        RemoveAura(50);
        UpdateRoundsLostUI();
        CommitProgress(); // Safe to save — round ended cleanly (even if lost)
        Debug.Log("[Progression] Round lost — progress committed.");
    }

    // ─────────────────────────────────────────────
    //  QUIT DETECTION
    // ─────────────────────────────────────────────

    void OnApplicationQuit()
    {
        HandleMidRoundQuit();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) HandleMidRoundQuit();
    }

    void HandleMidRoundQuit()
    {
        if (!isRoundInProgress) return;

        isRoundInProgress = false;

        // Revert in-memory stats to snapshot — mid-round XP/Aura earned is discarded
        totalXP   = xpSnapshot;
        totalAura = auraSnapshot;

        // Penalty for quitting
        totalXP   = Mathf.Max(0, totalXP   - 30);
        totalAura = Mathf.Max(0, totalAura - 50);
        roundsLost++;

        Debug.LogWarning("[Progression] Mid-round quit detected — reverting to snapshot + penalty applied.");

        // Save the penalized (but not mid-round-inflated) state
        CommitProgress();
    }

    // ─────────────────────────────────────────────
    //  CARD EVENTS
    // ─────────────────────────────────────────────

    public void OnCardSolved(float solveTime)
    {
        int xpGained = CalculateXP(solveTime);

        if (vfxManager != null && vfxManager.GetComboCount() >= 2)
            xpGained += comboXPBonus;

        AddXP(xpGained);

        if (solveTime < 4f)
            AddAura(10);

        // NOTE: We do NOT call CommitProgress() here.
        // XP/Aura are updated in memory and shown in UI,
        // but only written to disk at round end (OnRoundWon/OnRoundLost).
    }

    public void OnCardFailed()
    {
        cardsFailed++;
        RemoveXP(5);
        RemoveAura(20);
        UpdateCardsFailedUI();

        // cardsFailed IS saved immediately (it's a stat, not progression currency)
        PlayerPrefs.SetInt(KEY_CARDS_FAILED, cardsFailed);
        PlayerPrefs.SetString(KEY_CHECKSUM, BuildChecksum(totalXP, totalAura, cardsFailed, roundsLost));
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────
    //  SAVE / LOAD / CHECKSUM
    // ─────────────────────────────────────────────

    /// <summary>
    /// Only call this at round boundaries (win/lose/quit).
    /// Never call mid-round — that's the whole protection.
    /// </summary>
    void CommitProgress()
    {
        PlayerPrefs.SetInt(KEY_XP,           totalXP);
        PlayerPrefs.SetInt(KEY_AURA,         totalAura);
        PlayerPrefs.SetInt(KEY_CARDS_FAILED, cardsFailed);
        PlayerPrefs.SetInt(KEY_ROUNDS_LOST,  roundsLost);
        PlayerPrefs.SetString(KEY_CHECKSUM,  BuildChecksum(totalXP, totalAura, cardsFailed, roundsLost));
        PlayerPrefs.Save();
        Debug.Log($"[Progression] Committed — XP:{totalXP} Aura:{totalAura} Failed:{cardsFailed} Lost:{roundsLost}");
    }

    // Keep old name as alias so nothing else breaks
    void SaveProgress() => CommitProgress();

    void LoadAndVerifyProgress()
    {
        int savedXP      = PlayerPrefs.GetInt(KEY_XP,           0);
        int savedAura    = PlayerPrefs.GetInt(KEY_AURA,         0);
        int savedFailed  = PlayerPrefs.GetInt(KEY_CARDS_FAILED, 0);
        int savedLost    = PlayerPrefs.GetInt(KEY_ROUNDS_LOST,  0);
        string savedHash = PlayerPrefs.GetString(KEY_CHECKSUM, "");

        string expectedHash = BuildChecksum(savedXP, savedAura, savedFailed, savedLost);

        if (!string.IsNullOrEmpty(savedHash) && savedHash != expectedHash)
        {
            Debug.LogWarning("[Progression] Checksum mismatch — data tampered! Resetting.");
            totalXP = 0; totalAura = 0; cardsFailed = 0; roundsLost = 0;
            CommitProgress();
        }
        else
        {
            totalXP      = savedXP;
            totalAura    = savedAura;
            cardsFailed  = savedFailed;
            roundsLost   = savedLost;
        }

        UpdateAllUI();
    }

    string BuildChecksum(int xp, int aura, int failed, int lost)
    {
        return HashString($"{xp}|{aura}|{failed}|{lost}|SALT_KEY");
    }

    string HashString(string input)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    // ─────────────────────────────────────────────
    //  XP / AURA MATH
    // ─────────────────────────────────────────────

    int CalculateXP(float solveTime)
    {
        if (solveTime < 3f) return fastXP;
        else if (solveTime < 5f) return quickXP;
        else return normalXP;
    }

    void AddXP(int amount)
    {
        totalXP = Mathf.Max(0, totalXP + amount);
        UpdateXPUI();
        StartCoroutine(XPGainAnimation(amount));
    }

    void RemoveXP(int amount)
    {
        totalXP = Mathf.Max(0, totalXP - amount);
        UpdateXPUI();
        StartCoroutine(XPLossAnimation(amount));
    }

    void AddAura(int amount)
    {
        totalAura = Mathf.Max(0, totalAura + amount);
        UpdateAuraUI();
        StartCoroutine(AuraGainAnimation(amount));
    }

    void RemoveAura(int amount)
    {
        totalAura = Mathf.Max(0, totalAura - amount);
        UpdateAuraUI();
        StartCoroutine(AuraLossAnimation(amount));
    }

    // ─────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────

    void UpdateAllUI()       { UpdateXPUI(); UpdateAuraUI(); UpdateCardsFailedUI(); UpdateRoundsLostUI(); }
    void UpdateXPUI()        { if (xpText != null)         xpText.text         = $"{totalXP} xp"; }
    void UpdateAuraUI()      { if (auraText != null)        auraText.text       = $"{totalAura} aura"; }
    void UpdateCardsFailedUI(){ if (cardsFailedText != null) cardsFailedText.text = $"{cardsFailed} lost cards"; }
    void UpdateRoundsLostUI(){ if (roundsLostText != null)  roundsLostText.text = $"{roundsLost} lost rounds"; }

    // ─────────────────────────────────────────────
    //  ANIMATIONS (unchanged)
    // ─────────────────────────────────────────────

    IEnumerator XPGainAnimation(int amount)
    {
        if (xpText == null) yield break;
        xpText.color = xpGainColor;
        Vector3 orig = xpText.transform.localScale, target = orig * 1.2f;
        float e = 0f;
        while (e < xpGainAnimDuration / 2f) { e += Time.deltaTime; xpText.transform.localScale = Vector3.Lerp(orig, target, e / (xpGainAnimDuration / 2f)); yield return null; }
        e = 0f;
        while (e < xpGainAnimDuration / 2f) { e += Time.deltaTime; float t = e / (xpGainAnimDuration / 2f); xpText.transform.localScale = Vector3.Lerp(target, orig, t); xpText.color = Color.Lerp(xpGainColor, originalXPColor, t); yield return null; }
        xpText.transform.localScale = orig; xpText.color = originalXPColor;
    }

    IEnumerator XPLossAnimation(int amount)
    {
        if (xpText == null) yield break;
        xpText.color = xpLossColor;
        Vector3 orig = xpText.transform.localScale, target = orig * 0.8f;
        float e = 0f;
        while (e < xpGainAnimDuration / 2f) { e += Time.deltaTime; xpText.transform.localScale = Vector3.Lerp(orig, target, e / (xpGainAnimDuration / 2f)); yield return null; }
        e = 0f;
        while (e < xpGainAnimDuration / 2f) { e += Time.deltaTime; float t = e / (xpGainAnimDuration / 2f); xpText.transform.localScale = Vector3.Lerp(target, orig, t); xpText.color = Color.Lerp(xpLossColor, originalXPColor, t); yield return null; }
        xpText.transform.localScale = orig; xpText.color = originalXPColor;
    }

    IEnumerator AuraGainAnimation(int amount)
    {
        if (auraText == null) yield break;
        Vector3 orig = Vector3.one, target = orig * 1.3f;
        float s = Time.realtimeSinceStartup, half = xpGainAnimDuration / 2f;
        while (Time.realtimeSinceStartup - s < half) { float t = (Time.realtimeSinceStartup - s) / half; auraText.transform.localScale = Vector3.Lerp(orig, target, t); auraText.color = auraGainColor; yield return null; }
        s = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - s < half) { float t = (Time.realtimeSinceStartup - s) / half; auraText.transform.localScale = Vector3.Lerp(target, orig, t); auraText.color = Color.Lerp(auraGainColor, originalAuraColor, t); yield return null; }
        auraText.transform.localScale = orig; auraText.color = originalAuraColor;
    }

    IEnumerator AuraLossAnimation(int amount)
    {
        if (auraText == null) yield break;
        Vector3 orig = Vector3.one, target = orig * 0.8f;
        float s = Time.realtimeSinceStartup, half = xpGainAnimDuration / 2f;
        while (Time.realtimeSinceStartup - s < half) { float t = (Time.realtimeSinceStartup - s) / half; auraText.transform.localScale = Vector3.Lerp(orig, target, t); auraText.color = auraLossColor; yield return null; }
        s = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - s < half) { float t = (Time.realtimeSinceStartup - s) / half; auraText.transform.localScale = Vector3.Lerp(target, orig, t); auraText.color = Color.Lerp(auraLossColor, originalAuraColor, t); yield return null; }
        auraText.transform.localScale = orig; auraText.color = originalAuraColor;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC GETTERS / RESET
    // ─────────────────────────────────────────────

    public int GetTotalXP()     => totalXP;
    public int GetTotalAura()   => totalAura;
    public int GetCardsFailed() => cardsFailed;
    public int GetRoundsLost()  => roundsLost;

    public void ResetProgress()
    {
        totalXP = 0; totalAura = 0; cardsFailed = 0; roundsLost = 0;
        CommitProgress();
        UpdateAllUI();
        Debug.Log("Progress reset!");
    }
}