using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VoiceControl : MonoBehaviour {

    AudioClip myAudioClip;
    AudioSource audioSource;
    string micDevice;
    string lastError;

    void Start() {
        audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.Length < 1) {
            lastError = "Can't find a microphone device";
        } else if (Microphone.devices.Length == 1) {
            micDevice = Microphone.devices[0];
        }
    }

    void OnGUI() {
        if (lastError != null) {
            GUILayout.Label("Error: " + lastError);
        }
        if (micDevice != null) {
            ShowRecordMenu();
        } else {
            ShowSelectMicMenu();
        }
    }

    protected void ShowSelectMicMenu() {
        GUILayout.BeginArea(new Rect(0, 20, 200, 200));
        foreach (string device in Microphone.devices) {
            if (GUILayout.Button("> " + device)) {
                this.micDevice = device;
            }
        }
        GUILayout.EndArea();
    }

    protected void ShowRecordMenu() {
        GUILayout.BeginArea(new Rect(0, 20, 100, 100));

        bool isRecording = Microphone.IsRecording(micDevice);
        bool isPlaying = audioSource.isPlaying;

        if (!isRecording && !isPlaying) {
            if (GUILayout.Button("Record")) {
                this.myAudioClip = Microphone.Start(null, false, 10, 44100);
            }
        }

        if (isRecording) {
            if (GUILayout.Button("Stop Record")) {
                Microphone.End(this.micDevice);
                this.myAudioClip = SavWav.CreateClipByTrimmingSilence(this.myAudioClip, 0.0f);
            }
        }

        if (isPlaying) {
            if (GUILayout.Button("Stop")) {
                audioSource.Stop();
            }
        }

        if (!isPlaying && !isRecording && this.myAudioClip) {
            if (GUILayout.Button("Play")) {
                audioSource.clip = this.myAudioClip;
                audioSource.Play();
            }
        }

        if (!isRecording && this.myAudioClip) {
            if (GUILayout.Button("Save")) {
                string fileName = "VoiceRecord_" + VoiceControl.GetTimeStampStr() + ".wav";
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                SavWav.SaveToFile(filePath, this.myAudioClip);
            }
        }

        //if (!isRecording && this.myAudioClip) {
            if (GUILayout.Button("Send")) {
                string filePath = "/Users/dimgun/Library/Application Support/HoloGirls/HoloGirl/VoiceRecord_2018-05-27-11-17-27-8878.wav";
                StartCoroutine(SendRequest(filePath));
            }
        //}

        GUILayout.EndArea();
    }

    protected IEnumerator SendRequest(string filePath) {
        //const string uploadUrl = "https://api.ht.studsib.ru/voice-request";
        const string uploadUrl = "http://localhost:8080/voice-request";
        //const string uploadUrl = "http://httpbin.org/anything";
        byte[] fileData = File.ReadAllBytes(filePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection(
            name: "voice",
            data: fileData,
            fileName: "recorded-voice.wav",
            contentType: "audio/wav"
        ));

        // Construct a payload manually, to workaround UnityWebRequest issue with missed last boundary
        byte[] boundary = UnityWebRequest.GenerateBoundary();
        string boundaryStr = System.Text.Encoding.UTF8.GetString(boundary);
        // missed boundary consists of CRLF--{boundary}--
        byte[] lastBoundary = System.Text.Encoding.UTF8.GetBytes(System.String.Concat("\r\n--", boundaryStr, "--"));

        // Get raw form data
        byte[] formDataRaw = UnityWebRequest.SerializeFormSections(formData, boundary);

        //Append last boundary
        byte[] body = new byte[formDataRaw.Length + lastBoundary.Length];
        System.Buffer.BlockCopy(formDataRaw, 0, body, 0, formDataRaw.Length);
        System.Buffer.BlockCopy(lastBoundary, 0, body, formDataRaw.Length, lastBoundary.Length);

        // Add boundary to the content type, server won't detect parts otherwise
        string contentType = System.String.Concat("multipart/form-data; boundary=", boundaryStr);

        //Make a raw upload handler and set correct content type
        UploadHandler uploadHandler = new UploadHandlerRaw(body);
        uploadHandler.contentType = contentType;

        //Attach to web request
        UnityWebRequest req = new UnityWebRequest(uploadUrl);
        req.uploadHandler = uploadHandler;
        req.downloadHandler = new DownloadHandlerBuffer();
        req.method = UnityWebRequest.kHttpVerbPOST;

        req.chunkedTransfer = false;
        req.useHttpContinue = false;

        yield return req.SendWebRequest();

        if (req.isNetworkError || req.isHttpError) {
            Debug.Log("Failed to upload (code " + req.responseCode + "): " + req.error);
        } else {
            Debug.Log("Upload complete. Received in return (bytes): " + req.downloadedBytes);
            Debug.Log("Raw data" + req.downloadHandler.text);
        }
    }

    private static void DumpFormData(List<IMultipartFormSection> formData) {
        byte[] mulitypartRawData = UnityWebRequest.SerializeFormSections(
            formData,
            UnityWebRequest.GenerateBoundary()
        );

        string dumpFileName = "MultipartFormDataDump_" + VoiceControl.GetTimeStampStr() + ".txt";
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, dumpFileName);
        FileStream fileStream = new FileStream(filePath, FileMode.Create);
	    fileStream.Write(mulitypartRawData, 0, mulitypartRawData.Length);
	    fileStream.Close();
    }

    protected static string GetTimeStampStr() {
        return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff");
    }
}