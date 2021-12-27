using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class VoiceControl : MonoBehaviour {

    AudioClip myAudioClip;
    AudioSource audioSource;
    string micDevice;
    string lastError;
    bool shouldShowDebugMenu = false;

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

        if (micDevice == null) {
            ShowSelectMicMenu();
        } else if (shouldShowDebugMenu) {
            ShowDebugRecordMenu();
        } else {
            ShowInteractionMenu();
        }

        if (GUI.Button(new Rect(Screen.width - 100.0f, Screen.height-40.0f, 100.0f, 40.0f), "Debug")) {
            this.shouldShowDebugMenu = !this.shouldShowDebugMenu;
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

    protected void ShowInteractionMenu() {
        bool isRecording = Microphone.IsRecording(micDevice);
        bool isPlaying = audioSource.isPlaying;

        Rect buttonRect = new Rect(0, (Screen.height - 400.0f) * 0.5f, Screen.width, 400.0f);
        if(!isRecording && GUI.Button(buttonRect, "Ask")) {
            this.myAudioClip = Microphone.Start(null, false, 10, 44100);
        }

        if(isRecording && GUI.Button(buttonRect, "Send")) {
            Microphone.End(this.micDevice);
            this.myAudioClip = SavWav.CreateClipByTrimmingSilence(this.myAudioClip, 0.0f);
            byte[] myAudioClipData = SavWav.EncodeToByteArray(myAudioClip);
            StartCoroutine(SendVoiceRequest(myAudioClipData));
        }
    }

    protected void ShowDebugRecordMenu() {
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 20, Screen.height - 20));

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

        if (!isRecording && this.myAudioClip) {
            if (GUILayout.Button("Send")) {
                byte[] myAudioClipData = SavWav.EncodeToByteArray(myAudioClip);
                StartCoroutine(SendVoiceRequest(myAudioClipData));
            }
        }

        if (GUILayout.Button("Test request (not for Android)")) {
            string filePath = Application.streamingAssetsPath + "/test-whats-your-name.wav";
            byte[] fileData = File.ReadAllBytes(filePath);
            StartCoroutine(SendVoiceRequest(fileData));
        }

        GUILayout.EndArea();
    }

    protected IEnumerator SendVoiceRequest(byte[] wavFileData)
    {
        const string host = "http://localhost:8080";

        // Send request
        string metadataUri = null;
        string voiceAnswerUri = null;
        {
            TLog("N01. Sending voice request");
            string voiceRequestUri = "/voice-request";
            UnityWebRequest voiceRequest = CreateVoiceWebRequest(host + voiceRequestUri, wavFileData);
            yield return voiceRequest.SendWebRequest();
            TLogTextWebRequest(voiceRequest);

            TLog("N02. Parsing response for voice request");
            // Parse response to get voice and metadata urls
            string jsonResponseStr = voiceRequest.downloadHandler.text;
            if (!string.IsNullOrEmpty(jsonResponseStr)) {
                JSONNode json = JSON.Parse(jsonResponseStr);
                if (json != null) {
                    metadataUri = json["data"]["metadata"];
                    voiceAnswerUri = json["data"]["voice"];
                }
            }
        }

        if(string.IsNullOrEmpty(metadataUri)) {
            TLog("Failed to parse metadata URL from response");
            yield break;
        }

        // Get metadata
        bool voiceReady = false;
        string answerText = null;
        string questionText = null;
        {
            byte attemptsLeft = 3;
            JSONNode data = null;

            // Ask for answer for 3 times
            while(attemptsLeft > 0 && !voiceReady) {
                TLog("N02. Getting metadata. Attempts left: " + attemptsLeft);
                // Give server some time to process request
                yield return new WaitForSeconds(5.0f);

                UnityWebRequest metadataRequest = UnityWebRequest.Get(host + metadataUri);
                yield return metadataRequest.SendWebRequest();
                TLogTextWebRequest(metadataRequest);

                // Parse metadata to find if voice answer is ready
                string jsonResponseStr = metadataRequest.downloadHandler.text;
                if (jsonResponseStr != null && jsonResponseStr.Length > 0) {
                    JSONNode json = JSON.Parse(jsonResponseStr);
                    if (json != null) {
                        data = json["data"];
                        voiceReady = data["voice_ready"].AsBool;
                        questionText = data["text"].Value;
                        answerText = data["answer"].Value;
                    }
                }
                attemptsLeft--;
            }
        }

        if (!string.IsNullOrEmpty(answerText) || !string.IsNullOrEmpty(questionText)) {
            TLog("N03. Metadata fetched");
            string formatttedQuestion = string.IsNullOrEmpty(questionText) ? "¯\\_(ツ)_/¯" : questionText;
            TLog("@user> " + formatttedQuestion);
            TLog("@alla> " + answerText);
        }

        if (!voiceReady || string.IsNullOrEmpty(voiceAnswerUri)) {
            TLog("Server failed to prepare a voice answer.");
            yield break;
        }

        // Download voice answer
        {
            TLog("N04. Downloading voice answer");
            UnityWebRequest voiceAnswerRequest = UnityWebRequest.Get(host + voiceAnswerUri);
            DownloadHandlerAudioClip voiceAnswerDownloadHandler = new DownloadHandlerAudioClip(voiceAnswerUri, AudioType.WAV);
            voiceAnswerRequest.downloadHandler = voiceAnswerDownloadHandler;
            yield return voiceAnswerRequest.SendWebRequest();
            TLogBinaryWebRequest(voiceAnswerRequest);

            TLog("N05. Playing voice answer");
            //TODO: move out of here. It should just return audio data
            if (!voiceAnswerRequest.isNetworkError && !voiceAnswerRequest.isHttpError) {
                audioSource.clip = voiceAnswerDownloadHandler.audioClip;
                audioSource.Play();
            } else {
                TLog("Failed to play voice answer");
            }
        }
    }

    protected UnityWebRequest CreateVoiceWebRequest(string url, byte[] wavFileData) {
         List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection(
            name: "voice",
            data: wavFileData,
            fileName: "recorded-voice.wav",
            contentType: "audio/wav"
        ));

        UnityWebRequest req = VoiceControl.CreateMultipartFormDataWebRequest(url, UnityWebRequest.kHttpVerbPOST, formData);
        req.chunkedTransfer = false;
        req.useHttpContinue = false;

        return req;
    }

    // Constructs a multipart form web request with correct last boundary
    // Inspired by michaelneil's comment at
    // https://answers.unity.com/questions/1354080/unitywebrequestpost-and-multipartform-data-not-for.html
    protected static UnityWebRequest CreateMultipartFormDataWebRequest(string url, string method, List<IMultipartFormSection> sections) {
        byte[] boundary = UnityWebRequest.GenerateBoundary();
        string boundaryStr = System.Text.Encoding.UTF8.GetString(boundary);
        // missed boundary consists of CRLF--{boundary}--
        byte[] lastBoundary = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryStr + "--");

        // Get raw form data
        byte[] sectionsDataRaw = UnityWebRequest.SerializeFormSections(sections, boundary);

        //Append last boundary
        byte[] body = new byte[sectionsDataRaw.Length + lastBoundary.Length];
        System.Buffer.BlockCopy(sectionsDataRaw, 0, body, 0, sectionsDataRaw.Length);
        System.Buffer.BlockCopy(lastBoundary, 0, body, sectionsDataRaw.Length, lastBoundary.Length);

        // Add boundary to the content type, server won't detect parts otherwise
        string contentType = "multipart/form-data; boundary=" + boundaryStr;

        //Make a raw upload handler and set correct content type
        UploadHandler uploadHandler = new UploadHandlerRaw(body);
        uploadHandler.contentType = contentType;

        //Create a web request
        UnityWebRequest req = new UnityWebRequest(url);
        req.uploadHandler = uploadHandler;
        req.downloadHandler = new DownloadHandlerBuffer();
        req.method = method;

        return req;
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

    private static void TLogTextWebRequest(UnityWebRequest req)
    {
        if ( req == null ) return;
        string requestName = req.url;
        if (req.isNetworkError || req.isHttpError) {
            TLog(string.Format("Request '{0}' failed. Code: {1}. Error: {2}.", requestName, req.responseCode, req.error));
        } else {
            TLog(string.Format("Request '{0}' completed. Response text:{1}", requestName, req.downloadHandler.text));
        }
    }

    private static void TLogBinaryWebRequest(UnityWebRequest req)
    {
        if ( req == null ) return;
        string requestName = req.url;
        if (req.isNetworkError || req.isHttpError) {
            TLog(string.Format("Request '{0}' failed. Code: {1}. Error: {2}.", requestName, req.responseCode, req.error));
        } else {
            TLog(string.Format("Request '{0}' completed. Response size(bytes):{1}", requestName, req.downloadedBytes));
        }
    }

    protected static string GetTimeStampStr() {
        return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff");
    }

    static void TLog(string message) {
        string timestampStr = System.DateTime.Now.ToString("HH:mm:ss.ff");
        Debug.Log(timestampStr + " " + message);
    }
}
