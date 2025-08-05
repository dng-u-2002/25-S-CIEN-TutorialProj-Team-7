using System;
using UnityEngine;

namespace Voice
{
    public class VoiceReceiver : MonoBehaviour
    {
        private AudioSource audioSource;
        private float[] audioDataBuffer;
        private int sampleRate = 44100;
        private int channels = 1;

        private AudioClip clip;
        private int clipLengthSeconds = 1; // 1초 버퍼
        private int writePosition = 0;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            
            clip = AudioClip.Create("clip", sampleRate*clipLengthSeconds, channels, 1, false);
            audioSource.clip = clip;
            audioSource.Play();
            
            audioDataBuffer = new float[sampleRate*clipLengthSeconds];
        }

        public void OnVoiceDataReceived(byte[] recivedData)
        {
            int floatCount =  recivedData.Length/4;
            float[] data = new float[floatCount];
            Buffer.BlockCopy(recivedData,0,data,0,floatCount);

            for (int i = 0; i < data.Length; i++)
            {
                audioDataBuffer[i] = data[i];
                writePosition = (writePosition + 1)%audioDataBuffer.Length;
                //마이크 버퍼는 순환형이래오
            }
            clip.SetData(data, 0);
        }
    }
}