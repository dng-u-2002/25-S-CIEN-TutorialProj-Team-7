using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace BunnysLie_Server
{

    public enum ePacketSafefyLevel
    {
        //
        // Summary:
        //     Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        Unreliable = 4,
        //
        // Summary:
        //     Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
        ReliableUnordered = 0,
        //
        // Summary:
        //     Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
        Sequenced = 1,
        //
        // Summary:
        //     Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive
        //     in order.
        ReliableOrdered = 2,
        //
        // Summary:
        //     Reliable only last packet. Packets can be dropped (except the last one), won't
        //     be duplicated, will arrive in order. Cannot be fragmented
        ReliableSequenced = 3
    }

    public class NetworkDataReader
    {
        NetDataReader Reader;
        public NetworkDataReader(NetDataReader reader)
        {
            Reader = reader;
        }
        public string ReadString()
        {
            return Reader.GetString();
        }
        public int ReadInt()
        {
            return Reader.GetInt();
        }
        public float ReadFloat()
        {
            return Reader.GetFloat();
        }
        public bool ReadBool()
        {
            return Reader.GetBool();
        }
        public byte ReadByte()
        {
            return Reader.GetByte();
        }
        public byte ReadPacketType()
        {
            return Reader.GetByte();
        }
    }
    public class NetworkDataWriter
    {
        NetDataWriter Writer = new NetDataWriter();

        public void Recycle()
        {
            //Writer.rec
        }

        public void Clear()
        {
            Writer.Reset();
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
                Writer.Put(data);
            }
        }

        public void SendPacket(NetPeer peer)
        {
            if (Writer.Length > 0)
            {
                peer.Send(Writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void WriteString(string value)
        {
            Writer.Put(value);
        }
        public void WriteInt(int value)
        {
            Writer.Put(value);
        }
        public void WriteFloat(float value)
        {
            Writer.Put(value);
        }
        public void WriteBool(bool value)
        {
            Writer.Put(value);
        }
        public void WriteByte(byte value)
        {
            Writer.Put(value);
        }
        public void WritePacketType(byte type)
        {
            Writer.Put((byte)type);
        }
    }
}
