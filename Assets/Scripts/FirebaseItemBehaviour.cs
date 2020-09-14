using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class FirebaseItemBehaviour : MonoBehaviour
{
    //Populated in the inspector
    public TMP_Text _firebaseConnectionText;
    public TMP_InputField playerNameInput;
    public GameObject itemUIParent;
    public GameObject secondUISet;
    public GameObject firstUISet;

    private readonly UnityEvent _OnFirebaseInitialized = new UnityEvent();
    private FirebaseDatabase _database;

    private void Start()
    {
        //Add a listener to our Unity Event
        _OnFirebaseInitialized.AddListener(AfterFirebaseInitialization);
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

            //If successful, invoke the Unity Event, which calls EnablePlayButton()
            _OnFirebaseInitialized.Invoke();
        });
    }

    //Called at _OnFirebaseInitialized.Invoke()
    private void AfterFirebaseInitialization()
    {
        _firebaseConnectionText.text = "Connected to Firebase";

        //Retrieve the default database
        _database = FirebaseDatabase.DefaultInstance;
        _database.GetReference("Item").ValueChanged += HandleValueChanged;
    }

    //Remove event listener on destroy
    private void OnDestroy()
    {
        _database.GetReference("Item").ValueChanged -= HandleValueChanged;
        _database = null;
    }

    //Listener function for when DB is updated. Not used in this demo.
    private void HandleValueChanged(object sender, ValueChangedEventArgs e)
    {
        var json = e.Snapshot.GetRawJsonValue();
        //Debug.Log(json);
        if (!string.IsNullOrEmpty(json))
        {
            //var itemData = JsonUtility.FromJson<ItemData>(json);
            //UpdateGameStats();
        }
    }

    //This function is the OnClick() listener for the big item button in the scene
    public void ItemButtonFunc()
    {
        StartCoroutine(AfterAcquireTransaction());
    }

    //After the transaction is complete, check if the item owner in DB is the same as the entered player name
    //Else logic is handled in the AcquireItem() function
    private IEnumerator AfterAcquireTransaction()
    {
        var transactionTask = AcquireItem();
        yield return new WaitUntil(() => transactionTask.IsCompleted);

        if (transactionTask.Result == null)
        {
            Debug.Log("Inside AfterAcquireTransaction: No result from transactionTask");
            yield break;
        }

        Dictionary<string, object> result = (Dictionary<string, object>)transactionTask.Result.Value;
        if (playerNameInput.text.Equals(result["owner"]))
        {
            firstUISet.SetActive(false);
            secondUISet.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    //Transaction happens here.
    //NOTE: I had to remove all TransactionResult.Abort() statements because whenever the transaction failed, I would get a FireaseException: Rethrow AggregateException
    //Nothing is written to the DB if the acquired boolean is false, even though the transaction succeeds everytime.
    private async Task<DataSnapshot> AcquireItem()
    {
        return await _database.GetReference("Item").RunTransaction(mutableData =>
        {
            bool acquired;
            string owner;
            Dictionary<string, object> item = (Dictionary<string, object>)mutableData.Value;
            if (item != null)
            {
                acquired = (bool)item["acquired"];
                owner = (string)item["owner"];
                if (acquired == false)
                {
                    Dictionary<string, object> temp = new Dictionary<string, object>() {
                        {"acquired", true },
                        { "owner", playerNameInput.text }
                    };
                    mutableData.Value = temp;
                }
                else
                {
                    firstUISet.SetActive(false);
                    secondUISet.transform.GetChild(1).gameObject.SetActive(true);
                    secondUISet.transform.GetChild(1).GetComponent<TMP_Text>().text = "Sorry! " + owner + " acquired the item before you could.";
                }
            }

            return TransactionResult.Success(mutableData);
        });
    }

    //Shows the button and text elements once the player enters a name
    public void OnPlayerNameEntered()
    {
        if (playerNameInput.text.Equals(""))
        {
            itemUIParent.SetActive(false);
        }
        else
        {
            itemUIParent.SetActive(true);
        }
    }
}
