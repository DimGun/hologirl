using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveWave : MonoBehaviour {

    AudioClip myAudioClip;

    void Start () { }
    void Update () { }
    void OnGUI () {
        GUILayout.BeginArea(new Rect(0, 0, 100, 100));

        if (GUILayout.Button ("Record")) {
            myAudioClip = Microphone.Start (null, false, 10, 44100);
        }

        if (GUI.Button (new Rect (10, 70, 60, 50), "Save")) {

        if (GUILayout.Button ("Save")) {
            for (int i = 1;; i++)
                if (System.IO.File.Exists ("myfile")) {
                    VoiceRecord.Save ("myfile" + i, myAudioClip);
                }
            else {
                VoiceRecord.Save ("myfile", myAudioClip);
                //                      audio.Play();
            }
        }

        GUILayout.EndArea();
    }
}