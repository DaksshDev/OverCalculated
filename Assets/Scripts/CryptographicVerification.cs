using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

public class CryptographicVerification : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text deviceInfoText;

    [Header("Integrity Check")]
    [SerializeField] private string localFileName = "version.json";
    [SerializeField] private string githubRawURL = "https://raw.githubusercontent.com/YOUR_USER/YOUR_REPO/main/StreamingAssets/version.json";

    void Start()
    {
        // Show hashed device ID
        string deviceHash = HashString(SystemInfo.deviceUniqueIdentifier);
        deviceInfoText.text = $"Device ID: {deviceHash}";

        // Always check integrity on launch
        StartCoroutine(VerifyIntegrity());
    }

    IEnumerator VerifyIntegrity()
    {
        // Read local file directly (PC path, no APK gymnastics needed)
        string localPath = System.IO.Path.Combine(Application.streamingAssetsPath, localFileName);
        string localText = System.IO.File.ReadAllText(localPath);
        string localHash = HashString(localText);

        // Fetch from GitHub
        using (UnityWebRequest req = UnityWebRequest.Get(githubRawURL))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                AppendText("\n<color=yellow>(no connection)</color>");
                yield break;
            }

            string remoteHash = HashString(req.downloadHandler.text);

            if (localHash == remoteHash)
            {
                AppendText("\n<color=green>[official]</color>");
            }
            else
            {
                AppendText("\n<color=red>[Unofficial copy!]</color>");
            }
        }
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

    void AppendText(string msg) => deviceInfoText.text += msg;
}