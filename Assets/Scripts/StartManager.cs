using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform messageParent;

    public InputField userInput;
    public InputField passInput;
    public InputField roomInput;
    public Button joinButton;

    public GameObject colorWindow;
    public Button whiteButton;
    public Button blackButton;

    public string landName = "fog-chess";
    
    private string _landUrl;
    private string _realmUrl;

    private void Awake()
    {
        colorWindow.SetActive(false);
        joinButton.onClick.AddListener(Join);
        whiteButton.onClick.AddListener(White);
        blackButton.onClick.AddListener(Black);
        foreach (Transform child in messageParent) Destroy(child.gameObject);
    }

    public void Join()
    {
        Requests.SetCredentials(userInput.text, passInput.text);
        if (roomInput.text == "")
        {
            NewAlert("Invalid Key");
            return;
        }

        ServerManager.Realm = roomInput.text;
        StartCoroutine(JoinRealmRequest());
    }

    [Serializable]
    public class CreateRealmData
    {
        public string name;
    }

    [Serializable]
    public class GetRealmResponse : Requests.BaseResponse
    {
        public List<string> players;
        public string host;
    }

    [Serializable]
    public class RealmPropertiesResponse : Requests.BaseResponse
    {
        public List<RealmProperty> properties;
    }

    [Serializable]
    public class RealmProperty
    {
        public string player;
        public string name;
        public string value;
    }

    private IEnumerator JoinRealmRequest()
    {
        _landUrl = $"{Requests.BaseUrl}/lands/{landName}";
        _realmUrl = $"{_landUrl}/realms/{roomInput.text}";
        
        var request = Requests.Get($"{_realmUrl}/info");
        while (!request.isDone) yield return null;
        var response = Requests.GetResponse<GetRealmResponse>(request, false);
        switch (response.status)
        {
            case 200 when response.players.Count == 0:
                NewAlert("Invalid key");
                break;
            case 200 when response.players.Count == 1:
                request = Requests.Get($"{_realmUrl}/properties?name=HOST_COLOR");
                while (!request.isDone) yield return null;
                var propertiesResponse = Requests.GetResponse<RealmPropertiesResponse>(request);
                
                request = Requests.Get($"{_realmUrl}/join");
                while (!request.isDone) yield return null;
                var baseResponse = Requests.GetResponse<Requests.BaseResponse>(request, false);
                if (baseResponse.status != 200)
                {
                    NewAlert("Server error");
                    yield break;
                }

                GameManager.Color = propertiesResponse.properties[0].value == "White" ? "Black" : "White";
                ServerManager.Realm = roomInput.text;
                SceneManager.LoadScene(1);
                break;
            case 200 when response.players.Count == 2:
                NewAlert("Invalid key");
                break;
            case 401:
                NewAlert("Invalid user");
                break;
            case 404:
                colorWindow.SetActive(true);
                break;
            default:
                NewAlert("Server down");
                yield break;
        }
    }

    private void White() => StartCoroutine(SelectColorAndStart("White"));
    private void Black() => StartCoroutine(SelectColorAndStart("Black"));
    
    [Serializable]
    public class ColorData
    {
        public string value;
    }

    private IEnumerator SelectColorAndStart(string colorSelected)
    {
        GameManager.Color = colorSelected;
        ServerManager.Realm = roomInput.text;
        
        var request = Requests.Post($"{_landUrl}/realms/create", new CreateRealmData {name = roomInput.text});
        while (!request.isDone) yield return null;
        var response = Requests.GetResponse<GetRealmResponse>(request, false);
        if (response.status != 200)
        {
            NewAlert("Server error");
            yield break;
        }
        
        request = Requests.Post($"{_realmUrl}/properties/HOST_COLOR", new ColorData {value = colorSelected});
        while (!request.isDone) yield return null;
        Requests.GetResponse<RealmPropertiesResponse>(request);

        request = Requests.Get( $"{_realmUrl}/join");
        while (!request.isDone) yield return null;
        response = Requests.GetResponse<GetRealmResponse>(request, false);
        if (response.status != 200)
        {
            NewAlert("Server error");
            yield break;
        }

        SceneManager.LoadScene(1);
    }


    public void NewAlert(string txt)
    {
        var message = Instantiate(messagePrefab, messageParent).GetComponent<Text>();
        message.text = txt;
        Destroy(message.gameObject, 3);
    }
}