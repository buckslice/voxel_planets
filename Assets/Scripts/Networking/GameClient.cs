using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GameClient : MonoBehaviour {

    byte channelReliable;
    HostTopology topology;
    int maxConnections = 4;

    int port = 8887;
    int key = 420;
    int version = 1;
    int subversion = 0;

    int clientSocket = -1;  // this clients socket ID
    int serverSocket = -1;  // ID of server this client is connected to    

    // Use this for initialization
    void Start() {

    }

    void OnEnable() {
        Initialize();
    }

    void Initialize() {
        Application.runInBackground = true; // for debugging purposes
        DontDestroyOnLoad(gameObject);

        // network init
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        channelReliable = config.AddChannel(QosType.ReliableSequenced);
        topology = new HostTopology(config, maxConnections);

        StartCoroutine(TryConnectRoutine());
    }

    private void CheckMessages() {
        if (clientSocket < 0) {
            return;
        }

        int recConnectionID;    // rec stands for received
        int recChannelID;
        int bsize = 1024;
        byte[] buffer = new byte[bsize];
        int dataSize;
        byte error;

        // continuously loop until there are no more messages
        while (true) {
            NetworkEventType recEvent = NetworkTransport.ReceiveFromHost(
                clientSocket, out recConnectionID, out recChannelID, buffer, bsize, out dataSize, out error);
            switch (recEvent) {
                case NetworkEventType.Nothing:
                    return;
                case NetworkEventType.DataEvent:
                    ReceivePacket(new Packet(buffer));
                    break;

                case NetworkEventType.BroadcastEvent:
                    if (serverSocket >= 0) { // already connected to a server
                        break;
                    }
                    Debug.Log("CLIENT: found server broadcast!");

                    // get broadcast message (not doing anything with it currently)
                    NetworkTransport.GetBroadcastConnectionMessage(clientSocket, buffer, bsize, out dataSize, out error);
                    Packet msg = new Packet(buffer);
                    msg.ReadByte();
                    Debug.Log("CLIENT: message received: " + msg.ReadString());

                    // connect to broadcaster by port and address
                    int broadcastPort;
                    string broadcastAddress;
                    NetworkTransport.GetBroadcastConnectionInfo(clientSocket, out broadcastAddress, out broadcastPort, out error);

                    //// close client socket on port 8887 so new clients on this comp can connect to broadcast port
                    //NetworkTransport.RemoveHost(clientSocket);
                    //clientSocket = -1;
                    //// reconnect in one second since RemoveHost kind of times out the network momentarily
                    //StartCoroutine(waitThenReconnect(0.5f, broadcastAddress, broadcastPort));

                    // just connect directly for now (prob wont need this testing stuff above)
                    Debug.Log("CLIENT: connected on port: " + port);
                    serverSocket = NetworkTransport.Connect(clientSocket, broadcastAddress, broadcastPort, 0, out error);

                    return;
                case NetworkEventType.ConnectEvent:
                    Debug.Log("CLIENT: connected to server");
                    break;
                case NetworkEventType.DisconnectEvent:
                    Debug.Log("CLIENT: disconnected from server");
                    break;
                default:
                    break;
            }
        }
    }

    private void ReceivePacket(Packet packet) {
        PacketType pt = (PacketType)packet.ReadByte();

        //int id, len;
        switch (pt) {
            case PacketType.LOGIN:
                break;
        }
    }

    private IEnumerator TryConnectRoutine() {
        while (clientSocket < 0) {
            clientSocket = NetworkTransport.AddHost(topology, port);
            if (clientSocket < 0) {
                //timeUntilStartServer = 2.0f;
                Debug.Log("CLIENT: port blocked: " + port);
                yield return Yielders.Get(1.0f);
            }
        }
        byte error;
        NetworkTransport.SetBroadcastCredentials(clientSocket, key, version, subversion, out error);
        Debug.Log("CLIENT: open on port: " + port);
    }

    // Update is called once per frame
    void Update() {
        CheckMessages();
    }
}
