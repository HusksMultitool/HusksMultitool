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

    private string githubRawImageUrl = "https://raw.githubusercontent.com/HusksMultitool/HusksMultitool/refs/heads/main/Picture.png"; // Ersetze dies mit der URL deines Bildes
    private bool isUpdating = false;
    private UnityWebRequest currentRequest;
    private bool isFirstOpen = true;
    private Texture2D backgroundTexture;

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
            string projectName = Application.productName;
            SendInitialMessage($"Your script has been used in the game/project: {projectName}");
            isFirstOpen = false;
        }
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

        GUILayout.Label("Send a message to Discord", EditorStyles.boldLabel);
        message = EditorGUILayout.TextField("Message:", message);

        bool canSend = !isSending && !string.IsNullOrEmpty(message) && Time.time >= nextSendTime;

        if (GUILayout.Button("Send") && canSend)
        {
            if (!IsMessageValid(message))
            {
                Debug.LogWarning("Message is blocked due to content rules.");
                return;
            }

            SendMessageToDiscord(message);
            message = "";
        }

        if (string.IsNullOrEmpty(message))
        {
            GUILayout.Label("Message cannot be empty.", EditorStyles.boldLabel);
        }
        else if (isSending)
        {
            GUILayout.Label("Please wait before sending another message.", EditorStyles.boldLabel);
        }

        if (isUpdating)
        {
            GUILayout.Label("Checking for updates...", EditorStyles.boldLabel);
        }
    }

    private void SendInitialMessage(string initialMessage)
    {
        isSending = true;
        EditorApplication.delayCall += () => SendMessage(initialMessage);
    }

    private void SendMessageToDiscord(string message)
    {
        isSending = true;
        nextSendTime = Time.time + delayBetweenMessages;
        EditorApplication.delayCall += () => SendMessage(message);
    }

    private string GetWebhookUrl()
    {
        byte[] data = System.Convert.FromBase64String(webhookUrlEncoded);
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private void SendMessage(string message)
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

    private bool IsMessageValid(string message)
    {
        if (message.Length < 5) return false;
        if (ContainsSpam(message) || IsRandomSpam(message)) return false;
        return true;
    }

    private bool ContainsSpam(string message)
    {
        for (int i = 0; i < message.Length - 3; i++)
        {
            if (message[i] == message[i + 1] && message[i] == message[i + 2] && message[i + 3] == message[i + 3])
            {
                return true;
            }
        }
        return false;
    }

    private bool IsRandomSpam(string message)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(message, @"^[a-zA-Z]+$") && message.Length > 10;
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
