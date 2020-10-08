using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject cubePrefab;

    public string selfID;

    public List<Player> playerList = new List<Player>();

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();

        udp.Connect("52.15.65.92", 12345);
        //udp.Connect("localhost", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("PositionUpdate", 1, 0.3f);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        REMOVE_CLIENT
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        [Serializable]
        public struct receivedPosition
        {
            public float x;
            public float y;
            public float z;
        }
        public string id;
        public receivedColor color;
        public receivedPosition position;

        public GameObject cube;
        public bool hasCube = false;
        public bool hasUpdate = false;
        public bool hasDestroy = false;
    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    Player player = JsonUtility.FromJson<Player>(returnData);
                    playerList.Add(player);
                    selfID = player.id;
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    bool isListed = false;
                    int index = 0;
                    foreach(Player p1 in lastestGameState.players)
                    {
                        foreach(Player p2 in playerList)
                        {
                            if (p1.id == p2.id)
                            {
                                isListed = true;
                                index = playerList.IndexOf(p2);
                                break;
                            }
                        }

                        if(isListed)
                        {
                            p1.cube = playerList[index].cube;
                            p1.hasCube = playerList[index].hasCube;
                            p1.hasUpdate = true;
                            
                            playerList[index] = p1;
                        }
                        else
                        {
                            playerList.Add(p1);
                        }

                        isListed = false;
                    }
                    break;
                case commands.REMOVE_CLIENT:
                    foreach(Player p in playerList)
                    {
                        if(p.id == JsonUtility.FromJson<Player>(returnData).id)
                        {
                            p.hasDestroy = true;
                            break;
                        }
                    }
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(){
        foreach(Player p in playerList)
        {
            if(!p.hasCube)
            {
                //p.cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.cube = GameObject.Instantiate(cubePrefab);
                p.cube.transform.position = new Vector3(p.position.x, p.position.y, p.position.z);
                p.cube.GetComponent<MeshRenderer>().material.color = new Color(p.color.R, p.color.G, p.color.B);
                p.hasCube = true;
            }
        }
    }

    void UpdatePlayers(){
        foreach(Player p in playerList)
        {
            if (p.hasUpdate)
            {
                p.cube.GetComponent<MeshRenderer>().material.color = new Color(p.color.R, p.color.G, p.color.B);
                if (p.id == selfID)
                {
                    p.position.x = p.cube.transform.position.x;
                    p.position.y = p.cube.transform.position.y;
                    p.position.z = p.cube.transform.position.z;
                }
                p.hasUpdate = false;
            }
        }
    }

    void DestroyPlayers(){
        List<int> indices = new List<int>();

        foreach(Player p in playerList)
        {
            if(p.hasDestroy)
            {
                Destroy(p.cube);
                indices.Add(playerList.IndexOf(p));
            }
        }

        foreach(int i in indices)
        {
            playerList.RemoveAt(i);
        }
    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void PositionUpdate()
    {
        foreach(Player p in playerList)
        {
            if(p.id == selfID)
            {
                string json = JsonUtility.ToJson(p);
                Byte[] sendBytes = Encoding.ASCII.GetBytes(json);
                udp.Send(sendBytes, sendBytes.Length);
            }
        }
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
