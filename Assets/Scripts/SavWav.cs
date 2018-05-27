using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public static class SavWav {

	public static bool SaveToFile(string filepath, AudioClip clip) {
		Debug.Log("Writting wav file to '" + filepath + "'");

		// Make sure directory exists if user is saving to sub dir.
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));

		FileStream fileStream = new FileStream(filepath, FileMode.Create);
		Byte[] byteArray = EncodeToByteArray(clip);
		fileStream.Write(byteArray, 0, byteArray.Length);

		fileStream.Close();

		return true; // TODO: return false if there's a failure saving the file
	}

	public static AudioClip CreateClipByTrimmingSilence(AudioClip clip, float min) {
		if (clip.samples < 1) {
			return null;
		}

		var samples = new float[clip.samples * clip.channels];
		clip.GetData(samples, 0);

		var samplesTrimmed = TrimSilenceSafe(samples, min);
		var trimmedClip = AudioClip.Create("TempClip", samplesTrimmed.Length, clip.channels, clip.frequency, false);
		trimmedClip.SetData(samplesTrimmed, 0);
		return trimmedClip;
	}

	// Does the same as TrimSilence, but never returns null.
	public static float[] TrimSilenceSafe(float[] samples, float min) {
		float[] trimmedSamples = TrimSilence(samples, min);
		if (trimmedSamples != null && trimmedSamples.Length > 0) {
			return trimmedSamples;
		}

		return new float[1];
	}

	// Trims silence from both sides.
	public static float[] TrimSilence(float[] samples, float min) {
		float[] emptySamples = null;
		if (samples.Length < 1) {
			return emptySamples;
		}

		// TODO: If there are several channels, then we need to compare
		// them separately.
		Predicate<float> testIsLoud = (f) => { return (Mathf.Abs(f) > min); };
		int trimFrom = Array.FindIndex(samples, testIsLoud);
		int trimTo = Array.FindLastIndex(samples, testIsLoud);

		if (trimFrom == -1) { //All samples are silent (lower than `min`)
			return emptySamples;
		}

		int trimmedLength = trimTo - trimFrom + 1;
		float[] result = new float[trimmedLength];
		Array.Copy(samples, trimFrom, result, 0, trimmedLength);

		return result;
	}

	static Byte[] EncodeToByteArray(AudioClip clip) {
		Byte[] headerData = GetHeaderBytesArray(clip);

		float[] samples = new float[clip.samples * clip.channels];
		clip.GetData(samples, 0);

		Byte[] bytesData = new Byte[headerData.Length + samples.Length * 2];
		//bytesData array is twice the size of
		//dataSource array because a float converted in Int16 is 2 bytes.

		Int16[] intData = new Int16[samples.Length];
		//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

		const float rescaleFactor = 32767; //to convert float to Int16

		for (int i = 0; i < samples.Length; i++) {
			intData[i] = (short) (samples[i] * rescaleFactor);
			//Debug.Log (samples [i]);
		}

		Buffer.BlockCopy(headerData, 0, bytesData, 0, headerData.Length);
		Buffer.BlockCopy(intData, 0, bytesData, headerData.Length, intData.Length * 2);
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

		//UInt16 two = 2;
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