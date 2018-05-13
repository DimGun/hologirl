using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public static class SavWav {

	public static bool SaveToFile(string filepath, AudioClip clip) {
		Debug.Log("Writting wav file to '" + filepath + "'");

		// Make sure directory exists if user is saving to sub dir.
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));

		FileStream fileStream = new FileStream(filepath, FileMode.Create);
		Byte[] headerBytesArray = GetHeaderBytesArray(clip);
		fileStream.Write(headerBytesArray, 0, headerBytesArray.Length);

		Byte[] bodyByteArray = GetFileBodyByteArray(clip);
		fileStream.Write(bodyByteArray, 0, bodyByteArray.Length);
		fileStream.Close();

		return true; // TODO: return false if there's a failure saving the file
	}

	public static AudioClip TrimSilence(AudioClip clip, float min) {
		var samples = new float[clip.samples];

		clip.GetData(samples, 0);

		return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
	}

	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz) {
		return TrimSilence(samples, min, channels, hz, false, false);
	}

	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream) {
		int i;

		for (i=0; i<samples.Count; i++) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(0, i);

		for (i=samples.Count - 1; i>0; i--) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(i, samples.Count - i);

		var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

		clip.SetData(samples.ToArray(), 0);

		return clip;
	}

	static Byte[] GetFileBodyByteArray(AudioClip clip)
	{
		float[] samples = new float[clip.samples * clip.channels];
		clip.GetData (samples, 0);

		Int16[] intData = new Int16[samples.Length];
		//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

		Byte[] bytesData = new Byte[samples.Length * 2];
		//bytesData array is twice the size of
		//dataSource array because a float converted in Int16 is 2 bytes.

		const float rescaleFactor = 32767; //to convert float to Int16

		for (int i = 0; i < samples.Length; i++)
		{
			intData[i] = (short)(samples[i] * rescaleFactor);
			//Debug.Log (samples [i]);
		}
		Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
		return bytesData;
	}

	static Byte[] GetHeaderBytesArray(AudioClip clip) {

		const int HEADER_SIZE = 44;

		int hz = clip.frequency;
		int channels = clip.channels;
		int samples = clip.samples;

		MemoryStream headerMemStream = new MemoryStream();

		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		headerMemStream.Write(riff, 0, 4);

		var chunkSize = HEADER_SIZE + samples * channels * 2 - 4; // minus RIFF prefix
		Byte[] chunkSizeArr = BitConverter.GetBytes(chunkSize);
		headerMemStream.Write(chunkSizeArr, 0, 4);

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		headerMemStream.Write(wave, 0, 4);

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		headerMemStream.Write(fmt, 0, 4);

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		headerMemStream.Write(subChunk1, 0, 4);

		UInt16 two = 2;
		UInt16 one = 1;

		Byte[] audioFormat = BitConverter.GetBytes(one);
		headerMemStream.Write(audioFormat, 0, 2);

		Byte[] numChannels = BitConverter.GetBytes(channels);
		headerMemStream.Write(numChannels, 0, 2);

		Byte[] sampleRate = BitConverter.GetBytes(hz);
		headerMemStream.Write(sampleRate, 0, 4);

		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		headerMemStream.Write(byteRate, 0, 4);

		UInt16 blockAlign = (ushort) (channels * 2);
		headerMemStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		headerMemStream.Write(bitsPerSample, 0, 2);

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		headerMemStream.Write(datastring, 0, 4);

		Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
		headerMemStream.Write(subChunk2, 0, 4);

		return headerMemStream.ToArray();
	}
}