using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveWave : MonoBehaviour {

    AudioClip myAudioClip;

    void Start() { }
    void Update() { }
    void OnGUI() {
        GUILayout.BeginArea(new Rect(0, 0, 100, 100));

        bool isRecording = !Microphone.IsRecording();

        if (!isRecording) {
            if (GUILayout.Button("Record")) {
                myAudioClip = Microphone.Start(null, false, 10, 44100);
            }

            if (myAudioClip) {
                if (GUILayout.Button("Play")) {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    audioSource.clip = myAudioClip;
                    audioSource.Play();
                }

                if (GUILayout.Button("Save")) {
                    for (int i = 1;; i++)
                        if (System.IO.File.Exists("myfile")) {
                            VoiceRecord.Save("myfile" + i, myAudioClip);
                        }
                    else {
                        VoiceRecord.Save("myfile", myAudioClip);

                    }
                }
            }
        }

        if (isRecording) {
            if (GUILayout.Button("Stop Record")) {
                Microphone.End();
            }
        }

        GUILayout.EndArea();
    }
}