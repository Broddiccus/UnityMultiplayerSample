using UnityEngine;
using System.Collections.Generic;
public class Messages{
    public static string AddPlayer(Player player){
        var MessageBoy = new Message();
        MessageBoy.cmd = Commands.NEW_CLIENT;
        MessageBoy.players = new Player[]{ player };
        return JsonUtility.ToJson(MessageBoy);
    }

    public static string DisconnectPlayers(List<Player> dConnP)
    {
        var MessageBoy = new Message();
        MessageBoy.cmd = Commands.DELETE;
        MessageBoy.players = dConnP.ToArray();
        return JsonUtility.ToJson(MessageBoy);
    }

    public static string Update(Dictionary<int, Player> activePlayers)
    {
        var MessageBoy = new Message();
        MessageBoy.cmd = Commands.UPDATE;
        MessageBoy.players = new Player[activePlayers.Count];
        activePlayers.Values.CopyTo(MessageBoy.players, 0);
        return JsonUtility.ToJson(MessageBoy);
    }

    public static string UpdateOthers(Dictionary<int, Player> activePlayers){
        var MessageBoy = new Message();
        MessageBoy.cmd = Commands.OTHERS;
        MessageBoy.players = new Player[activePlayers.Count];
        activePlayers.Values.CopyTo(MessageBoy.players, 0);
        return JsonUtility.ToJson(MessageBoy);
    }

    public static string UpdatePosition(Movement movePlayer)
    {
        var MessageBoy = new Message();
        MessageBoy.cmd = Commands.MOVEMENT;
        MessageBoy.movePlayer = movePlayer;
        return JsonUtility.ToJson(MessageBoy);
    }

    

    

    public static string Heartbeat(){
        var MessageObj = new Message();
        MessageObj.cmd = Commands.HEARTBEAT;
        MessageObj.random = Random.Range(0f,100f);
        return JsonUtility.ToJson(MessageObj);
    }
}