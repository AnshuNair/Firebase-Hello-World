using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Firebase;
using Firebase.Extensions;
using TMPro;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class FirebaseInit : MonoBehaviour
{
    //Populated in the inspector
    public TMP_Text _firebaseConnectionText;
    public GameObject _errorTextObj;
    public GameObject _firstUISet;
    public GameObject _secondUISet;
    public Button _playButton;
    public TMP_InputField _playerNameField;
    public TMP_InputField _matchNumberField;

    private object lastGameData;

    private readonly UnityEvent _OnFirebaseInitialized = new UnityEvent();
    private FirebaseDatabase _database;
    private string _gameNum;
    private bool _inGame = false;

    private void Start()
    {
        //Add a listener to our Unity Event
        _OnFirebaseInitialized.AddListener(EnablePlayButton);
        _firebaseConnectionText.text = "Connecting...";

        //Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            //Get error message if there was an error initializing
            if (task.Exception != null)
            {
                _firebaseConnectionText.text = "Failed to connect";
                Debug.LogError($"Failed to initialize Firebase with {task.Exception}");
                return;
            }

            //If successful, invoke the Unity Event, which calls EnablePlayButton
            _OnFirebaseInitialized.Invoke();
        });
    }

    private void EnablePlayButton()
    {
        _playButton.interactable = true;
        _firebaseConnectionText.text = "Connected to Firebase";

        //Retrieve the default database
        _database = FirebaseDatabase.DefaultInstance;
    }

    public void GameButtonFunc()
    {
        StartCoroutine(JoinOrLeaveMatch());
    }

    private IEnumerator JoinOrLeaveMatch()
    {
        if (!_inGame)
        {
            //Check value of playername inputfield. If empty, change color to red and return out
            if (!_playerNameField.text.Equals(""))
            {
                //If value of match number inputfield is not empty, and match number already exists, then join that match (increase numPlayers by 1).
                //Otherwise, enable error text and return out.
                if (!_matchNumberField.text.Equals(""))
                {
                    _gameNum = _matchNumberField.text;
                    var currentNumPlayers = GetNumberOfPlayersInGame(_matchNumberField.text);
                    yield return new WaitUntil(() => currentNumPlayers.IsCompleted);

                    if (!currentNumPlayers.Result.Exists)
                    {
                        _errorTextObj.SetActive(true);
                        yield break;
                    }

                    long value = (long)currentNumPlayers.Result.Value;
                    long newValue = value + 1;
                    GameData gd = new GameData(newValue);

                    //Write to the database
                    var writeNumPlayers = SetNumberOfPlayersInGame(_matchNumberField.text, gd);
                    yield return new WaitUntil(() => writeNumPlayers.IsCompleted);

                    //Hide error text if enabled
                    if (_errorTextObj.activeSelf)
                    {
                        _errorTextObj.SetActive(false);
                    }

                    _inGame = true;
                    SwitchUI(_matchNumberField.text, newValue.ToString());
                }
                //If empty, get all active matches and create a match with match number equal to number of matches
                else
                {
                    var currentGames = GetAllGames();
                    yield return new WaitUntil(() => currentGames.IsCompleted);

                    //Count the number of games
                    long i = 0;
                    foreach (DataSnapshot ds in currentGames.Result.Children)
                    {
                        i++;
                    }

                    //Dictionary<string, int> newGameEntry = new Dictionary<string, int> { { "NumPlayers", 1 } };
                    GameData gd = new GameData(1L);

                    //Write to the database
                    var writeNewGame = SetNumberOfPlayersInGame((i + 1).ToString(), gd);
                    yield return new WaitUntil(() => writeNewGame.IsCompleted);

                    _inGame = true;
                    _gameNum = i.ToString();
                    SwitchUI(i.ToString(), 1.ToString());
                }
            }
            else
            {
                Debug.Log("Player name is empty");
            }
        }
        //Handle leave game logic here. Reduce numPlayers by 1 and switch the UI
        else
        {
            var currentNumPlayers = GetNumberOfPlayersInGame(_gameNum);
            yield return new WaitUntil(() => currentNumPlayers.IsCompleted);

            if (!currentNumPlayers.Result.Exists)
            {
                _errorTextObj.SetActive(true);
                yield break;
            }

            string value = currentNumPlayers.Result.Value.ToString();
            int newValue = int.Parse(value) - 1;

            //Dictionary<string, int> newGameEntry = new Dictionary<string, int> { { "NumPlayers", newValue } };
            GameData gd = new GameData(newValue);

            //Write to the database
            var writeNumPlayers = SetNumberOfPlayersInGame(_gameNum, gd);
            yield return new WaitUntil(() => writeNumPlayers.IsCompleted);

            _inGame = false;
            SwitchUI();
            _gameNum = "";
        }
    }

    private async Task<DataSnapshot> GetNumberOfPlayersInGame(string childPath)
    {
        var currentNumPlayers = await _database
                        .GetReference("Games")
                        .Child(childPath)
                        .Child("NumPlayers")
                        .GetValueAsync();

        return currentNumPlayers;
    }

    private async Task<DataSnapshot> GetAllGames()
    {
        var currentGames = await _database
                        .GetReference("Games")
                        .GetValueAsync();

        return currentGames;
    }

    private Task SetNumberOfPlayersInGame(string childPath, GameData gd)
    {
        if (!gd.Equals(lastGameData))
        {
            Task task = _database
                 .GetReference("Games")
                 .Child(childPath)
                 .Child("NumPlayers")
                 .SetValueAsync(gd.NumPlayers);

            return task;
        }

        return null;
    }

    //Change the UI after the player creates/joins a game or leaves one
    public void SwitchUI(string gameNumber = "", string numPlayers = "")
    {
        if (_inGame)
        {
            _firstUISet.SetActive(false);
            _secondUISet.SetActive(true);
            if (!gameNumber.Equals("") && !gameNumber.Equals(""))
            {
                _secondUISet.transform.GetChild(0).GetComponent<TMP_Text>().text = "Hello " + _playerNameField.text +
                    "! Welcome to game: " + gameNumber.ToString();
                _secondUISet.transform.GetChild(1).GetComponent<TMP_Text>().text = "Number of Players here: " + numPlayers.ToString();
            }
            _playButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Leave Game";
        }
        else
        {
            _firstUISet.SetActive(true);
            _matchNumberField.text = "";
            _playerNameField.text = "";
            _secondUISet.SetActive(false);
            _secondUISet.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
            _secondUISet.transform.GetChild(1).GetComponent<TMP_Text>().text = "";
            _playButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Create Game";
        }
    }

    //Change the text of the game button depending on whether anything was entered in the match number input field.
    //A listener to the _OnFirebaseInitialized Unity Event
    public void ChangePlayButtonText()
    {
        if (_matchNumberField.text.Equals(""))
        {
            _playButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Create Game";
            if (_errorTextObj.activeSelf)
            {
                _errorTextObj.SetActive(false);
            }
        }
        else
        {
            _playButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Join Game";
        }
    }
}
