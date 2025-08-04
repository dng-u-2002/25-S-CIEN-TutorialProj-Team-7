using System;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Voice
{
    public class VoiceSender : MonoBehaviour
    {
        public NetPeer peer;
        public string microphoneDevice;
        public AudioClip audioClip;
        private bool transmitted = false;
        
        private int lastSample = 0;
        private int sampleRate = 44100;
        private int recordLength = 1;
        private NetDataWriter writer = new  NetDataWriter();

        void Start()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.Log("마이크가 읍습니다");
                return;
            }
            
            microphoneDevice = Microphone.devices[0];
            audioClip = Microphone.Start(microphoneDevice, true, 1, sampleRate);
            transmitted = true;
        }

        void Update()
        {
            if (peer == null || !transmitted || peer.ConnectionState == ConnectionState.Connected)
            {
                return;
            }
            int currentSample = Microphone.GetPosition(microphoneDevice);
            int sampleCount = currentSample - lastSample;
            if (sampleCount <= 0) return;
            
            float[] samples = new float[sampleCount*audioClip.channels];
            audioClip.GetData(samples, lastSample);
            byte[] bytes = FloatArrayToByte(samples);
            
            writer.Reset();
            writer.Put((byte)PacketType.Voice);  // 패킷 타입
            writer.Put(bytes.Length);            // 길이 명시
            writer.Put(bytes);                   // 음성 데이터
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            lastSample = currentSample;
        }
        
        public enum PacketType
        {
            Chat = 1,
            Voice = 2,
            Login = 3
        }

        public byte[] FloatArrayToByte(float[] samples)
        {
            byte[] bytes = new byte[samples.Length*4];
            for (int i = 0; i < samples.Length; i++)
            {
                byte[] data = System.BitConverter.GetBytes(samples[i]);
                System.Buffer.BlockCopy(samples, 0, bytes, i*4, 4);
            }

            return bytes;
        }
    }
}