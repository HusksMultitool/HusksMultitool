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

    private string inputKey = "";
    private int incorrectAttempts = 0;
    private const string validKeyEncoded = "SHVza3NTeXN0ZW09LTcyMyImwqcvVUhXSik4SzM4Mg=="; // Encodierter Schlüssel
    private bool isKeyValid = false;

    private UnityWebRequest updateRequest;
    private bool isUpdating = false;

    [MenuItem("Tools/Husk Multitool")]
    public static void ShowWindow()
    {
        var window = GetWindow<HuskMultitoolWindow>("Husk Multitool");
        window.CheckForUpdates();
    }

    private void OnEnable()
    {
        ShowInitialMessage();
    }

    private void ShowInitialMessage()
    {
        Debug.Log("Thanks for sucking my cock!");
    }

    private void OnGUI()
    {
        GUILayout.Label("Thanks for using Husk Multitool!", EditorStyles.boldLabel);

        if (!isKeyValid)
        {
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

        if (isKeyValid && !isSending)
        {
            message = EditorGUILayout.TextField("Message:", message);

            if (GUILayout.Button("Send to Discord"))
            {
                SendMessageToDiscord(message);
                message = ""; // Clear the message field after sending
            }
        }

        if (isUpdating)
        {
            GUILayout.Label("Checking for updates...", EditorStyles.label);
        }
    }

    private void ValidateKey(string key)
    {
        string decodedKey = DecodeBase64(validKeyEncoded);

        if (key == decodedKey)
        {
            Debug.Log("Key validated successfully!");
            isKeyValid = true; // Key ist korrekt, weitere Funktionen aktivieren
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
        updateRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/HusksMultitool/HusksMultitool/refs/heads/main/HuskMultitoolWindow.cs");
        updateRequest.SendWebRequest();
        EditorApplication.update += UpdateCheckForUpdates;
    }

    private void UpdateCheckForUpdates()
    {
        if (updateRequest.isDone)
        {
            if (updateRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading file: " + updateRequest.error);
            }
            else
            {
                string localFilePath = "Assets/Editor/HuskMultitoolWindow.cs";
                string remoteContent = updateRequest.downloadHandler.text;
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
            updateRequest.Dispose();
            EditorApplication.update -= UpdateCheckForUpdates;
        }
    }
}
