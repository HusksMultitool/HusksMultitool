using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class HuskMultitoolWindow : EditorWindow
{
    private string message = "";
    private string webhookUrlEncoded = "aHR0cHM6Ly9kaXNjb3JkLmNvbS9hcGkvd2ViaG9va3MvMTI5OTAxODAxNDM2ODIwNjkwMS9rLTZLN0dUNURIYVgwSkJ3QnFhYUp6Mkc5Mjgwc1RUeXdhZzRlcGFiTnNmeXRqMVNLaThWWlhZVzRwNXZ2Uzl4VkRzbA==";
    private bool isSending = false;
    private float delayBetweenMessages = 5f;
    private float nextSendTime = 0f;
    private string githubRawImageUrl = "https://raw.githubusercontent.com/HusksMultitool/HusksMultitool/refs/heads/main/Picture.png";
    private bool isUpdating = false;
    private UnityWebRequest currentRequest;
    private bool isFirstOpen = true;
    private Texture2D backgroundTexture;

    private string inputKey = "";
    private int incorrectAttempts = 0;
    private const string validKeyEncoded = "SHVza3NTeXN0ZW09LTcyMyImwqcvVUhXSik4SzM4Mg=="; // Encodierter Schlüssel

    [MenuItem("Tools/Husk Multitool")]
    public static void ShowWindow()
    {
        var window = GetWindow<HuskMultitoolWindow>("Husk Multitool");
        window.CheckForUpdates();
    }

    private void OnEnable()
    {
        DownloadBackgroundImage();
        if (isFirstOpen)
        {
            ShowInitialMessage();
            isFirstOpen = false;
        }
    }

    private void ShowInitialMessage()
    {
        Debug.Log("Thanks for using Husk Multitool!");
    }

    private void DownloadBackgroundImage()
    {
        currentRequest = UnityWebRequestTexture.GetTexture(githubRawImageUrl);
        currentRequest.SendWebRequest();
        EditorApplication.update += UpdateCheck;
    }

    private void UpdateCheck()
    {
        if (currentRequest.isDone)
        {
            if (currentRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading image: " + currentRequest.error);
            }
            else
            {
                backgroundTexture = DownloadHandlerTexture.GetContent(currentRequest);
                Debug.Log("Background image downloaded successfully!");
            }

            currentRequest.Dispose();
            EditorApplication.update -= UpdateCheck;
        }
    }

    private void OnGUI()
    {
        // Hintergrund zeichnen
        if (backgroundTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, position.width, position.height), backgroundTexture);
        }

        GUILayout.Label("Thanks for using Husk Multitool!", EditorStyles.boldLabel);
        
        if (incorrectAttempts < 3)
        {
            GUILayout.Label("Please enter a valid key:", EditorStyles.label);
            inputKey = EditorGUILayout.TextField("Key:", inputKey);

            if (GUILayout.Button("Submit"))
            {
                ValidateKey(inputKey);
            }
        }
        else
        {
            GUILayout.Label("Maximum attempts exceeded. Please wait...", EditorStyles.label);
        }
    }

    private void ValidateKey(string key)
    {
        string decodedKey = DecodeBase64(validKeyEncoded);

        if (key == decodedKey)
        {
            Debug.Log("Key validated successfully!");
            // Hier kannst du den Rest des Fensters anzeigen
        }
        else
        {
            incorrectAttempts++;
            Debug.LogWarning("Invalid key attempt!");

            if (incorrectAttempts >= 3)
            {
                string projectName = Application.productName;
                string message = $"Invalid key entered 3 times. Project: {projectName}, Timestamp: {System.DateTime.Now}";
                SendMessageToDiscord(message);
                Debug.Log("Notification sent to Discord!");
            }
        }
    }

    private string DecodeBase64(string encoded)
    {
        byte[] data = System.Convert.FromBase64String(encoded);
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private void SendMessageToDiscord(string message)
    {
        isSending = true;
        nextSendTime = Time.time + delayBetweenMessages;
        EditorApplication.delayCall += () => SendDiscordMessage(message);
    }

    private void SendDiscordMessage(string message)
    {
        string json = "{\"content\": \"" + message + "\"}";
        var request = new UnityWebRequest(GetWebhookUrl(), "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var asyncOperation = request.SendWebRequest();
        asyncOperation.completed += operation =>
        {
            isSending = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error sending message: " + request.error);
            }
            else
            {
                Debug.Log("Message sent successfully!");
            }
        };
    }

    private string GetWebhookUrl()
    {
        byte[] data = System.Convert.FromBase64String(webhookUrlEncoded);
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private void CheckForUpdates()
    {
        if (isUpdating) return;
        isUpdating = true;

        Debug.Log("Checking for updates...");
        currentRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/HusksMultitool/HusksMultitool/refs/heads/main/HuskMultitoolWindow.cs");
        currentRequest.SendWebRequest();
        EditorApplication.update += UpdateCheckForUpdates;
    }

    private void UpdateCheckForUpdates()
    {
        if (currentRequest.isDone)
        {
            if (currentRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading file: " + currentRequest.error);
            }
            else
            {
                string localFilePath = "Assets/Editor/HuskMultitoolWindow.cs";
                string remoteContent = currentRequest.downloadHandler.text;
                string localContent = File.Exists(localFilePath) ? File.ReadAllText(localFilePath) : "";

                if (remoteContent != localContent)
                {
                    File.WriteAllText(localFilePath, remoteContent);
                    AssetDatabase.Refresh();
                    Debug.Log("File updated successfully!");
                }
                else
                {
                    Debug.Log("Nothing new found.");
                }
            }

            isUpdating = false;
            currentRequest.Dispose();
            EditorApplication.update -= UpdateCheckForUpdates;
        }
    }
}
