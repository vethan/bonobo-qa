using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;


public class Testupload : MonoBehaviour
{
    public class APIResponse
    {
        public string url;
        public Dictionary<string, string> data;
    }

    public static string prolificID;
    public static Testupload uploader = null;

    private byte[] lastReplay = null;
    private Dictionary<string, float> coreMetrics = null;

    string participantId = Guid.NewGuid().ToString();
    public bool isUploading;

    public void SetGameplayData(MemoryStream replay, Dictionary<string, float> gameMetrics)
    {
        lastReplay = replay.ToArray();
        coreMetrics = gameMetrics;
    }

    public void SetCommentsAndUpload(bool found, string comments)
    {
        isUploading = true;
        Dictionary<string, object> postDictionary = new Dictionary<string, object>();
        foreach (var kvp in coreMetrics)
        {
            postDictionary[kvp.Key] = kvp.Value;
        }

        postDictionary["prolificID"] = prolificID;
        postDictionary["session_id"] = participantId;
        postDictionary["reported_bug_found"] = found.ToString();
        postDictionary["comments"] = comments;
        postDictionary["created_at"] = DateTime.UtcNow.ToString();
        var json = JsonConvert.SerializeObject(postDictionary);

        StartCoroutine(Upload(json, lastReplay));
    }

    private void Awake()
    {
        if (uploader != null && uploader != this)
        {
            GameObject.Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        uploader = this;
    }

    // Start is called before the first frame update
    void Start()
    {
    }


    IEnumerator Upload(string jsonBody, byte[] file)
    {
        Debug.Log(jsonBody);
        UnityWebRequest www =
            new UnityWebRequest("https://u3bhq2c678.execute-api.eu-west-2.amazonaws.com/api/store", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            isUploading = false;
            yield break;
        }

        var data = JsonConvert.DeserializeObject<APIResponse>(www.downloadHandler.text);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        foreach (var kvp in data.data)
        {
            formData.Add(new MultipartFormDataSection(kvp.Key, kvp.Value));
        }

        formData.Add(
            new MultipartFormFileSection("file", file, data.data["key"], "application/octet-stream"));

        www = UnityWebRequest.Post(data.url, formData);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }

        isUploading = false;
    }

    // Update is called once per frame
    void Update()
    {
    }
}