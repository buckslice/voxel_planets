using UnityEngine;
using System.IO;

// packet types
// byte put at beginning of packet indicating what type of message it is
// can use this to determine order to read back data in
public enum PacketType : byte {
    LOGIN,
    MESSAGE,
    CHAT_MESSAGE,
    STATE_UPDATE,
    SPAWN_BOMB,
    SPAWN_POWERUP,
    PLAYER_DIED,
    GAME_END,
    GAME_COUNTDOWN,
    PLAYER_JOINED_ROOM,
    PLAYER_LEFT_ROOM,
    PLAYER_JOINED_SERVER,
    PLAYER_LEFT_SERVER,
    YOU_JOINED_ROOM,
    CHANGE_ROOM,
    ROOM_LIST_UPDATE,
    SET_READY,
}

public class Packet {

    private byte[] buffer;

    private MemoryStream stream;

    private BinaryWriter writer = null;
    private BinaryReader reader = null;
    /// <summary>
    /// Build a packet for writing data with default buffer size of 1024 bytes
    /// </summary>
    public Packet(PacketType type) {
        buffer = new byte[1024];
        stream = new MemoryStream(buffer, true);
        writer = new BinaryWriter(stream);
        Write((byte)type);
    }
    /// <summary>
    /// Build a packet for writing data with specified buffer size in bytes
    /// </summary>
    public Packet(PacketType type, int size) {
        buffer = new byte[size];
        stream = new MemoryStream(buffer, true);
        writer = new BinaryWriter(stream);
        Write((byte)type);
    }
    /// <summary>
    /// Build a packet for reading data
    /// </summary>
    /// <param name="buffer">data to be read</param>
    public Packet(byte[] buffer) {
        stream = new MemoryStream(buffer, false);
        reader = new BinaryReader(stream);
    }

    //// build a packet as a copy of another packet
    //public Packet(Packet p) {
    //    if (p.stream.CanWrite) {
    //        buffer = new byte[p.buffer.Length];
    //        System.Array.Copy(p.buffer, 0, buffer, 0, p.stream.Position);
    //        stream = new MemoryStream(buffer);
    //        stream.Seek(p.stream.Position, SeekOrigin.Begin);
    //        writer = new BinaryWriter(stream);
    //    } else {
    //        //stream = new MemoryStream(p.stream.GetBuffer(), false);
    //        //stream.Seek(p.stream.Position, SeekOrigin.Begin);
    //        //reader = new BinaryReader(stream);
    //        stream = p.stream;
    //        reader = p.reader;
    //    }
    //}

    // add more methods here as needed
    public void Write(string s) {
        writer.Write(s);
    }
    public void Write(byte b) {
        writer.Write(b);
    }
    public void Write(bool b) {
        writer.Write(b);
    }
    public void Write(int i) {
        writer.Write(i);
    }
    public void Write(float f) {
        writer.Write(f);
    }
    public void Write(Vector3 v) {
        writer.Write(v.x);
        writer.Write(v.y);
        writer.Write(v.z);
    }
    public void Write(Quaternion q) {
        writer.Write(q.x);
        writer.Write(q.y);
        writer.Write(q.z);
        writer.Write(q.w);
    }
    public void Write(Color32 c) {
        writer.Write(c.r);
        writer.Write(c.g);
        writer.Write(c.b);
        writer.Write(c.a);
    }

    public string ReadString() {
        return reader.ReadString();
    }
    public byte ReadByte() {
        return reader.ReadByte();
    }
    public bool ReadBool() {
        return reader.ReadBoolean();
    }
    public int ReadInt() {
        return reader.ReadInt32();
    }
    public float ReadFloat() {
        return reader.ReadSingle();
    }
    public Vector3 ReadVector3() {
        return new Vector3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }
    public Quaternion ReadQuaternion() {
        return new Quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }
    public Color32 ReadColor() {
        return new Color32(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte());
    }

    /// <summary>
    /// Returns packet data to be sent over network
    /// </summary>
    /// <returns></returns>
    public byte[] getData() {
        return buffer;
    }
    /// <summary>
    /// Returns length of data
    /// </summary>
    /// <returns></returns>
    public int getSize() {
        return (int)stream.Position;
    }

}
