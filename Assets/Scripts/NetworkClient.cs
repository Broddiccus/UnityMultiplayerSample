using UnityEngine;
using System.Text;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;

public class NetworkClient : MonoBehaviour
{
    public string serverAddress = "127.0.0.1";
    public ushort serverPort = 12666;
    public float moveSpeed = 10f;
    public UdpNetworkDriver networkDriver;
    public NetworkConnection Connection;
    public bool Finished;
    private GameObject RotatingCubePrefab;

    private Dictionary<string, GameObject> Cubes = new Dictionary<string, GameObject>();
    void Start ()
    {
        RotatingCubePrefab = Resources.Load("CubeOBJ", typeof(GameObject)) as GameObject;
        networkDriver = new UdpNetworkDriver(new INetworkParameter[0]);
        Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverAddress, serverPort);
        Connection = networkDriver.Connect(endpoint);
        InvokeRepeating("HeartBeat", 1, 1);  
    }

    

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        if (!Connection.IsCreated)
        {
            if (!Finished)
                Debug.Log("connection didn't work.");
            return;
        }

        bool sendMove = false;
        var loc = new Movement();
        if (Input.GetKey(KeyCode.A)){
            sendMove = true;
            loc.x += -moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D)){
            sendMove = true;
            loc.x += moveSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.W)){
            sendMove = true;
            loc.y = moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)){
            sendMove = true;
            loc.y = -moveSpeed * Time.deltaTime;
        }
        if(sendMove) {
            string message = Messages.UpdatePosition(loc);
            Sender.Send(message, networkDriver, Connection);
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = Connection.PopEvent(networkDriver, out stream)) !=
               NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                var infoBuffer = new byte[stream.Length];
                stream.ReadBytesIntoArray(ref readerCtx, ref infoBuffer, stream.Length);
                var resultString = Encoding.ASCII.GetString(infoBuffer);
                Debug.Log("Got " + resultString + " from the Server");
                var message = Decoder.Decode(resultString);
                if(message.cmd == Commands.OTHERS){
                    foreach(Player pl in message.players) {
                        GameObject newCube = Instantiate(
                            RotatingCubePrefab,
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            ), 
                            Quaternion.Euler(0, 0, 0)) as GameObject;
                        NetworkCube currentCube = newCube.GetComponent<NetworkCube>();
                        currentCube.id = pl.id;
                        currentCube.ChangeColor(pl.color.R, pl.color.G, pl.color.B);
                        Cubes.Add(pl.id, newCube);
                    }
                } else if (message.cmd == Commands.NEW_CLIENT){
                    foreach(Player pl in message.players) {
                        GameObject newCube = Instantiate(
                            RotatingCubePrefab,
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            ), 
                            Quaternion.Euler(0, 0, 0)) as GameObject;
                        NetworkCube currentCube = newCube.GetComponent<NetworkCube>();
                        currentCube.id = pl.id;
                        currentCube.ChangeColor(pl.color.R, pl.color.G, pl.color.B);
                        Cubes.Add(pl.id, newCube);
                    }
                } else if (message.cmd == Commands.UPDATE) {
                    foreach(Player pl in message.players) {
                        GameObject cube = Cubes[pl.id];
                        cube.transform.position = 
                            new Vector3(
                                pl.position.x,
                                pl.position.y,
                                pl.position.z
                            );
                    }
                } else if (message.cmd == Commands.DELETE) {
                    foreach(Player pl in message.players) {
                        if(Cubes.ContainsKey(pl.id))
                        {
                            GameObject cube = Cubes[pl.id];
                            Destroy(cube);
                            Cubes.Remove(pl.id);
                        }
                    }

                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client has disconnected");
                Connection = default(NetworkConnection);
                Finished = true;

            }
        }
    }

    void HeartBeat()
    {
        string message = Messages.Heartbeat();
        Sender.Send(message, networkDriver, Connection);
    }

    public void OnDestroy()
    {
        Connection.Disconnect(networkDriver);
        Finished = true;
        networkDriver.Dispose();
    }
}