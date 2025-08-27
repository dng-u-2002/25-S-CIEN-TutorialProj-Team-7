using ExitGames.Client.Photon;
using Helpers;
using LiteNetLib.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VOYAGER_Server;
using static InGameServer_PUN;
using static UnityEngine.Rendering.DebugUI;


public class NetworkDataWriter_PUN
{
	List<object> Data = new List<object>();
	byte packetType = 0;
	public void Recycle()
	{
		//Writer.rec
	}

	public void Clear()
	{
		packetType = 0;
		Data.Clear();
	}

	public void CreateNewPacket(byte type)
	{
		Clear();
		WritePacketType(type);
	}

	public void WriteByteArray(byte[] data)
	{
		if (data != null && data.Length > 0)
		{
			for (int i = 0; i < data.Length; i++)
			{
				Data.Add(data[i]);
			}
		}
	}

	public void SendPacket(InGameServer_PUN.User peer)
	{
		Photon.Pun.PhotonNetwork.RaiseEvent(packetType, Data.ToArray(), new RaiseEventOptions
		{
			TargetActors = new int[] { peer.Player.ActorNumber },
		}, SendOptions.SendReliable);
	}

	public void WriteString(string value)
	{
		Data.Add(value);
	}
	public void WriteInt(int value)
	{
		Data.Add(value);
	}
	public void WriteFloat(float value)
	{
		Data.Add(value);
	}
	public void WriteBool(bool value)
	{
		Data.Add(value);
	}
	public void WriteByte(byte value)
	{
		Data.Add(value);
	}
	public void WritePacketType(byte type)
	{
		//Data.Add(type);
		packetType = type;
	}
}

public class NetworkDataReader_PUN
{
	object[] Reader;
	int NowCounter = 0;
	public NetworkDataReader_PUN(object reader)
	{
		Reader = (object[])reader;
	}
	object GetNext()
	{
		if (NowCounter >= Reader.Length)
		{
			Debug.LogError($"[NetworkDataReader_PUN] Attempted to read past the end of the data. NowCounter: {NowCounter}, Reader Length: {Reader.Length}");
			return null;
		}
		return Reader[NowCounter++];
	}
	public string ReadString()
	{
		return (string)GetNext();
	}
	public int ReadInt()
	{
		return (int)GetNext();
	}
	public float ReadFloat()
	{
		return (float)GetNext();
	}
	public bool ReadBool()
	{
		return (bool)GetNext();
	}
	public byte ReadByte()
	{
		return (byte)GetNext();
	}
	public byte ReadPacketType()
	{
		return (byte)GetNext();
	}
}