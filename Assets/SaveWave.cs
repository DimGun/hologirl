using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveWave : MonoBehaviour {

    AudioClip myAudioClip;
    string micDevice;
    string lastError;

    void Start() {
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

        if (!isRecording) {
            if (GUILayout.Button("Record")) {
                this.myAudioClip = Microphone.Start(null, false, 10, 44100);
            }
        }

        if (!isRecording && this.myAudioClip) {
            if (GUILayout.Button("Play")) {
                AudioSource audioSource = GetComponent<AudioSource>();
                audioSource.clip = this.myAudioClip;
                audioSource.Play();
            }

            if (GUILayout.Button("Save")) {
                string fileName = "VoiceRecord_" + GetTimeStampStr();
                VoiceRecord.Save(fileName, this.myAudioClip);
            }
        }

        if (isRecording) {
            if (GUILayout.Button("Stop Record")) {
                Microphone.End(this.micDevice);
            }
        }

        GUILayout.EndArea();
    }

    protected string GetTimeStampStr() {
        return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff");
    }
}