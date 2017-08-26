using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class GameServer : MonoBehaviour {

    byte channelReliable;
    int maxConnections = 16;

    int port = 8888;
    int key = 420;
    int version = 1;
    int subversion = 0;

    int serverSocket = -1;  // my server socket id

    // list of connected clients
    private List<int> clients = new List<int>();


    // Use this for initialization
    void Start() {

    }

    void OnEnable() {
        Initialize();
    }

    void Initialize() {
        Application.runInBackground = true; // for debugging purposes
        DontDestroyOnLoad(gameObject);

        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        channelReliable = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);

        serverSocket = NetworkTransport.AddHost(topology, port);
        Debug.Log("SERVER: socket opened: " + serverSocket);

        Packet p = new Packet(PacketType.MESSAGE);
        p.Write("Hi its the server broadcasting!");

        byte error;
        bool success = NetworkTransport.StartBroadcastDiscovery(
            serverSocket, port - 1, key, version, subversion,
            p.getData(), p.getSize(), 500, out error);

        if (!success) {
            Debug.Log("SERVER: start broadcast discovery failed!");
        } else if (NetworkTransport.IsBroadcastDiscoveryRunning()) {
            Debug.Log("SERVER: started and broadcasting");
        } else {
            Debug.Log("SERVER: started but not broadcasting!?");
        }

    }

    private void CheckMessages() {
        int recConnectionID;    // rec stands for received
        int recChannelID;
        int bsize = 1024;
        byte[] buffer = new byte[bsize];
        int dataSize;
        byte error;

        while (true) {
            NetworkEventType recEvent = NetworkTransport.ReceiveFromHost(
                serverSocket, out recConnectionID, out recChannelID, buffer, bsize, out dataSize, out error);
            switch (recEvent) {
                case NetworkEventType.Nothing:
                    return;
                case NetworkEventType.DataEvent:
                    ReceivePacket(new Packet(buffer), recConnectionID);
                    break;
                case NetworkEventType.ConnectEvent:
                    clients.Add(recConnectionID);
                    Debug.Log("SERVER: client connected: " + recConnectionID);
                    break;
                case NetworkEventType.DisconnectEvent:
                    clients.Remove(recConnectionID);
                    Debug.Log("SERVER: client disconnected: " + recConnectionID);
                    //removeFromPlayers(recConnectionID);

                    break;
                default:
                    break;

            }
        }

    }

    private void ReceivePacket(Packet packet, int clientID) {
        PacketType packetType = (PacketType)packet.ReadByte();
        //Packet retPack;   // return packet
        switch (packetType) {
            case PacketType.LOGIN:
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update() {
        CheckMessages();
    }
}
