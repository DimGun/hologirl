﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                string fileName = "VoiceRecord_" + GetTimeStampStr() + ".wav";
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                SavWav.SaveToFile(filePath, this.myAudioClip);
            }
        }

        if (isRecording) {
            if (GUILayout.Button("Stop Record")) {
                Microphone.End(this.micDevice);
                this.myAudioClip = SavWav.CreateClipByTrimmingSilence(this.myAudioClip, 0.0f);
            }
        }

        GUILayout.EndArea();
    }

    protected string GetTimeStampStr() {
        return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff");
    }
}