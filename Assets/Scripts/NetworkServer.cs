using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;


public class NetworkServer : MonoBehaviour
{
    public UdpNetworkDriver m_Driver;
    private NativeList<NetworkConnection> Connections;
    private Dictionary<int, Player> m_Players = new Dictionary<int, Player>();
    private List<Player> m_DisconnectedPlayers = new List<Player>();
    private bool dirty = false;

    void Start ()
    {
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12666;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port");
        else
            m_Driver.Listen();
        Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        InvokeRepeating("HeartBeat", 1, 1);  
    }

    void HeartBeat(){
        string message = Messages.Heartbeat();
        for(int i = 0; i < Connections.Length; i++) {
            Sender.Send(message, m_Driver, Connections[i]);
        }
    }


    void SendDisconnectedPlayers()
    {
        string message = Messages.DisconnectPlayers(m_DisconnectedPlayers);
        for(int i = 0; i < Connections.Length; i++) {
            Sender.Send(message, m_Driver, Connections[i]);
        }
    }
    public void OnDestroy()
    {
        for (int i = 0; i < Connections.Length; i++){
            m_DisconnectedPlayers.Add(m_Players[Connections[i].InternalId]);
            m_Players.Remove(Connections[i].InternalId);
        }
        SendDisconnectedPlayers();
        m_Driver.Dispose();
        Connections.Dispose();
    }
    void CleanConnect()
    {
        bool oneDown = false;
        for (int i = 0; i < Connections.Length; i++)
        {
            if (!Connections[i].IsCreated)
            {
                m_DisconnectedPlayers.Add(m_Players[Connections[i].InternalId]);
                m_Players.Remove(Connections[i].InternalId);
                Debug.Log("Connection lost with " + Connections[i].InternalId);
                Connections.RemoveAtSwapBack(i);
                --i;
                oneDown = true;
            }
        }
        if(oneDown){
            SendDisconnectedPlayers();
        }
    }
    void SendNewChallenger(Player addPlayerStuff)
    {
        string message = Messages.AddPlayer(addPlayerStuff);
        for(int i = 0; i < Connections.Length; i++) {
            Sender.Send(message, m_Driver, Connections[i]);
        }
    }
    void SendFirstUpdateMessage(NetworkConnection c)
    {
        string message = Messages.UpdateOthers(m_Players);
        Sender.Send(message, m_Driver, c);
    }
    void SendUpdateMessage()
    {
        string message = Messages.Update(m_Players);
        for(int i = 0; i < Connections.Length; i++) {
            Sender.Send(message, m_Driver, Connections[i]);
        }
    }
    void NewConnect (){
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            var currentPlayer = Player.NewPlayer(c.InternalId);
            if(Connections.Length > 0){
                SendNewChallenger(currentPlayer);
            }
            Connections.Add(c);
            m_Players.Add(c.InternalId, currentPlayer);
            SendFirstUpdateMessage(c);
        }
    }

    void ReceiveData(int connIdx){
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        var conn = Connections[connIdx];

        while ((cmd = m_Driver.PopEventForConnection(conn, out stream)) !=
                NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                var infoBuffer = new byte[stream.Length];
                stream.ReadBytesIntoArray(ref readerCtx, ref infoBuffer, stream.Length);
                var resultString = Encoding.ASCII.GetString(infoBuffer);
                Debug.Log("Got " + resultString + " from the Client: " + conn.InternalId);
                var message = Decoder.Decode(resultString);
                if (message != null && message.cmd == Commands.MOVEMENT){
                    dirty = true;
                    if(m_Players.ContainsKey(conn.InternalId))
                    {
                        m_Players[conn.InternalId].position.x += message.movePlayer.x;
                        m_Players[conn.InternalId].position.y += message.movePlayer.y;
                    }
                    else{
                        Debug.Log("Cound not find player with Id: " + conn.InternalId);
                    }
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client removed from server: " + conn.InternalId);
                conn = default(NetworkConnection);
                m_DisconnectedPlayers.Add(m_Players[Connections[connIdx].InternalId]);
                m_Players.Remove(Connections[connIdx].InternalId);
                Debug.Log("Connection lost with " + Connections[connIdx].InternalId);
                Connections.RemoveAtSwapBack(connIdx);
                SendDisconnectedPlayers();
            }
        }
    }
    void CheckConnect()
    {
        for (int i = 0; i < Connections.Length; i++)
        {
            if (!Connections[i].IsCreated){
                Assert.IsTrue(true);
            }
            ReceiveData(i);
        }
        if(dirty){
            dirty = false;
            SendUpdateMessage();
        }
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();
        CleanConnect();
        NewConnect();
        CheckConnect();
    }
}