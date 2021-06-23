using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    public static readonly List<Msg> MessagesToRead = new List<Msg>();
    public static readonly List<Msg> MessagesToSend = new List<Msg>();

    [Serializable]
    public class Msg
    {
        public string topic;
        public List<string> payload;
        public bool read;
    }

    [Serializable]
    private class MsgData
    {
        public List<Msg> messages = new List<Msg>();
    }

    [Serializable]
    private class MsgResponse : Requests.BaseResponse
    {
        public string date;
        public List<Msg> messages = new List<Msg>();
    }

    public static string Land = "fog-chess";
    public static string Realm = "test";
    public static string RealmUrl;

    private GameManager _gm;
    private string _fromDate;
    private readonly object _receiveMessagesFrequency = new WaitForSecondsRealtime(1f);
    private readonly object _sendMessagesFrequency = new WaitForSecondsRealtime(1f);

    private IEnumerator ReceiveMessages()
    {
        while (Requests.ServerReady)
        {
            yield return _receiveMessagesFrequency;

            var url = $"{RealmUrl}/receive?topic=actions&from={_fromDate}&persistence=0";
            var request = Requests.Get(url);
            while (!request.isDone)
                yield return null;

            var response = Requests.GetResponse<MsgResponse>(request);
            if (!response)
            {
                Debug.LogError(response.message);
                continue;
            }

            MessagesToRead.AddRange(response.messages);
            _fromDate = response.date;
        }
    }

    private IEnumerator SendMessages()
    {
        while (Requests.ServerReady)
        {
            yield return _sendMessagesFrequency;

            if (!MessagesToSend.Any()) continue;

            var messages = MessagesToSend.ToList();
            var data = new MsgData {messages = messages};
            MessagesToSend.Clear();

            var url = $"{RealmUrl}/publish";
            var request = Requests.Post(url, data);
            while (!request.isDone)
                yield return null;

            Requests.GetResponse<MsgResponse>(request, false);
        }
    }

    private void ReadMessages()
    {
        var newMessages = MessagesToRead.Where(a => !a.read).ToList();
        foreach (var message in newMessages)
            ResolveMessage(message);

        var actionsToDelete = MessagesToRead.Where(a => a.read).ToList();
        if (actionsToDelete.Any())
            MessagesToRead.Remove(actionsToDelete[0]);
    }

    private void ResolveMessage(Msg message)
    {
        var action = message.payload[0];
        var tiles = _gm.Tiles;
        var piece = GameManager.Instance.pieces.First(x => x.name == message.payload[1]);
        Tile destTile;
        
        try
        {
            switch (action)
            {
                case "move":
                    destTile = message.payload[2] switch
                    {
                        "w" => GameManager.Instance.whiteCapturedTiles[int.Parse(message.payload[3])],
                        "b" => GameManager.Instance.blackCapturedTiles[int.Parse(message.payload[3])],
                        _ => tiles[int.Parse(message.payload[2]), int.Parse(message.payload[3])]
                    };
                    
                    Debug.Log($"{piece.tile.name} ({piece.color} {piece.type}) moved to {destTile.name}");
                    destTile.PlacePiece(piece);
                    break;
                
                case "capture":
                    destTile = message.payload[2] switch
                    {
                        "w" => GameManager.Instance.whiteCapturedTiles[int.Parse(message.payload[3])],
                        "b" => GameManager.Instance.blackCapturedTiles[int.Parse(message.payload[3])],
                        _ => tiles[int.Parse(message.payload[2]), int.Parse(message.payload[3])]
                    };
                    
                    Debug.Log($"{piece.tile.name} ({piece.color} {piece.type}) captured " +
                              $"{destTile.name} ({destTile.piece.color} {destTile.piece.type})");
                    _gm.AddToCaptured(destTile.piece);
                    destTile.PlacePiece(piece);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Message (payload={string.Join("|", message.payload)}) throws error: {e}");
        }
        finally
        {
            message.read = true;
        }
    }

    private void Awake()
    {
        _gm = GameManager.Instance;
    }

    private void Start()
    {
        Requests.ServerReady = true;

        RealmUrl = $"{Requests.BaseUrl}/lands/{Land}/realms/{Realm}";
        _fromDate = GameManager.NowIsoDate();
        StartCoroutine(ReceiveMessages());
        StartCoroutine(SendMessages());
    }

    private void Update()
    {
        ReadMessages();
    }
}