using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;

public class MatchHandler : MonoBehaviour
{
    // Match State
    public bool _matchStartedClient = false;
    public bool _matchStartedHost = false;
    //private bool _matchInitialized = false;
    public float _roundChangeTime; // When the change will happen

    private enum Rounds {Lobby, Initializing, Voting, Waiting, GameOver}
    Rounds _roundCurrent = Rounds.Voting;
    Rounds _roundLast = Rounds.Voting;

    private float _upateLast;

    // References
    DatabaseReference _reference;
    [SerializeField] GameObject _characterListGO;
    [SerializeField] GameObject _countDownTimer;
    [SerializeField] GameObject _roundTitle;
    [HideInInspector] public User _userLocal;
    [HideInInspector] public Match _matchLocal;
    public List<string> _secretnamesReference = new List<string>();
    public List<GameObject> _avatarList = new List<GameObject>();
    public List<string> _secretActionReference = new List<string>();

    [HideInInspector] public GameObject playerNameLabel;

    [HideInInspector] public List<User> _userList = new List<User>();
    [HideInInspector] public GameObject _voteButton;
    [HideInInspector] public GameObject _viewListButton;

    [HideInInspector] public GameObject _gameOverScreen;
    [HideInInspector] public GameObject _gameOverWinnerText;

    [HideInInspector] public GameObject playerSentPopup;
    [HideInInspector] public GameObject playerSentText;

    [HideInInspector] public GameObject newPlayerHasListPopup;

    [HideInInspector] public GameObject secretNameListPanel;
    [HideInInspector] public GameObject secretNameListText;

    void Start()
    {
        _reference = FirebaseDatabase.DefaultInstance.RootReference;
        _countDownTimer = GameObject.Find("Round Timer");
        _roundTitle = GameObject.Find("Round Title");
        _characterListGO = GameObject.Find("Characters");
        DontDestroyOnLoad(_characterListGO);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > _upateLast + 0.2f)
        {
            UpdateSlow(); // Use less data
        }
    }

    private void UpdateSlow()
    {
        // In "Lobby" - waiting for players to ready up
        //if (_matchInitialized == false && _matchStarted == false)
        //{
        //    ReadyCheck();
        //}

        //if (_matchInitialized == false && _matchStarted == true)
        //{
        //    _roundCurrent = Rounds.Initializing;
        //    //TO-DO: Get round change time from database
        //}


        if (_matchStartedHost == true)
        {
            MatchUpdateHost();
            if (_countDownTimer != null) _countDownTimer.GetComponent<Text>().text = (_matchLocal.RoundTimer).ToString();
        }
        if (_matchStartedClient == true)
        {
            MatchUpdateClient();
            if (_countDownTimer != null) _countDownTimer.GetComponent<Text>().text = (_matchLocal.RoundTimer).ToString();
        }
    }

    private void ReadyCheck()
    {

    }

    public void MatchStart(bool host)
    {
        Debug.Log("Votingo Round has started");
        _roundLast = Rounds.GameOver;
        _roundCurrent = Rounds.Voting;
        _matchLocal.RoundCurrent = 1;
        StartCoroutine(LateInit());

        if (host)
        {
            _roundChangeTime = Time.time + 10f;
            _matchStartedHost = true;
            _matchStartedClient = false;
        }
        else
        {
            _matchStartedHost = false;
            _matchStartedClient = true;
        }
    }

    private IEnumerator LateInit ()
    {
        yield return new WaitForSeconds(0.2f);
        _characterListGO.transform.position = GameObject.Find("Character Anchor").transform.position; //Move characters to center of screen
        GameObject.Find("Left Button").GetComponent<Button>().onClick.AddListener(delegate { GetComponent<RealtimeDatabase>().AvatarNext(); });
        GameObject.Find("Right Button").GetComponent<Button>().onClick.AddListener(delegate { GetComponent<RealtimeDatabase>().AvatarLast(); });
        GameObject.Find("Vote Button").GetComponent<Button>().onClick.AddListener(delegate { GetComponent<RealtimeDatabase>().AvatarVote(); });
        GameObject.Find("Send Button").GetComponent<Button>().onClick.AddListener(delegate { GetComponent<RealtimeDatabase>().AvatarSend(); });

        yield return new WaitForSeconds(1f);
        _countDownTimer = GameObject.Find("Round Timer");
        _roundTitle = GameObject.Find("Round Title");
        _voteButton = GameObject.Find("Vote Button");
        _viewListButton = GameObject.Find("Scroll List True Names Button");
        _viewListButton.SetActive(false);

        playerNameLabel = GameObject.Find("PlayerNameLabel");

        _gameOverScreen = GameObject.Find("Game Over Up Panel");
        _gameOverWinnerText = GameObject.Find("Winner Text");
        _gameOverScreen.SetActive(false);

        playerSentPopup = GameObject.Find("Player Sent Announcement Panel");
        playerSentText = GameObject.Find("Player Sent Announcement Text");
        playerSentPopup.SetActive(false);

        newPlayerHasListPopup = GameObject.Find("New List Owner Popup");
        newPlayerHasListPopup.SetActive(false);

        secretNameListPanel = GameObject.Find("Scroll List True Names Panel");
        secretNameListText = GameObject.Find("Scroll List True Names Text");
        secretNameListPanel.SetActive(false);

        string listText = "";
       foreach(User user in _userList)
       {
            listText += user.UserName + " - " + user.SecretName + "\n";
       }
        secretNameListText.GetComponent<Text>().text = listText;
    }

    private void MatchUpdateHost()
    {
        UsersLocalUpdate(); // Update the local user info from Database

        //_matchLocal.RoundTimer = _roundChangeTime;
        //Debug.Log(_matchLocal.RoundTimer);
        if (Time.time > _roundChangeTime)
        {
            switch (_roundCurrent)
            {
                /*
                case Rounds.Initializing:
                    Debug.Log("Voting Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting starts in:";
                    _roundChangeTime = Time.time + 10f;
                    _matchLocal.RoundCurrent = 2; //Move to Voting Round
                    _roundCurrent = Rounds.Voting;
                    break;
                */

                case Rounds.Voting:
                    // total the votes
                    Dictionary<string, int> voteCountByUsername = new Dictionary<string, int>();
                    foreach( User user in _userList)
                    {
                        int newTotal = 0;
                        voteCountByUsername.TryGetValue(user.UserName, out newTotal);
                        newTotal++;
                        voteCountByUsername[user.CurrentVote] = newTotal;
                    }
                    // get winner
                    KeyValuePair<string, int> highestPair;
                    foreach (KeyValuePair<string, int> entry in voteCountByUsername)
                    {
                        if(!highestPair.Equals(default(KeyValuePair<string, int>)))
                        {
                            if( highestPair.Value > entry.Value )
                            {
                                highestPair = entry;
                            }
                        }
                        else
                        {
                            highestPair = entry;
                        }
                    }
                    string winningUsername = highestPair.Key;
                    _matchLocal.ListHolderUserName = winningUsername;
                    newPlayerHasListPopup.SetActive(true);

                    Debug.Log("Waiting Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting starts in:";
                    _roundChangeTime = Time.time + 10f;
                    _matchLocal.RoundCurrent = 3; //Move to Waiting Round
                    _roundCurrent = Rounds.Waiting;
                    break;

                case Rounds.Waiting:
                    Debug.Log("Voting Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting ends in:";
                    //TO-DO: Check for game over
                    _roundChangeTime = Time.time + 60f;
                    _matchLocal.RoundCurrent = 2; //Move to Voting Round
                    _roundCurrent = Rounds.Voting;
                    break;

                case Rounds.GameOver:
                    Debug.Log("Game Over Round has started");
                    _roundChangeTime = Time.time + 10f;
                    _matchLocal.RoundCurrent = 1; //Move to Initializing Round
                    //TO-DO: Return to main menu
                    break;
            }
        }

        _matchLocal.RoundTimer = _roundChangeTime - Time.time;

        // Save to Database
        string json = JsonUtility.ToJson(_matchLocal);
        //Debug.Log(json);

        _reference.Child("Match").SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                //Debug.Log("Successfully added data to Firebase.");
            }
            else
            {
                //Debug.Log("Adding data failed.");
            }
        });
    }

    private void MatchUpdateClient ()
    {
        // Read from Database
        _reference.Child("Match").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                //Debug.Log(snapshot.Child("ActionAscend").Value.ToString());
                Match match = JsonUtility.FromJson<Match>(snapshot.GetRawJsonValue());
                _matchLocal = match; // Overwrite local Match class
                //Debug.Log(_matchLocal.ActionAscend);
                // enable list viewing if we got voted and aren't sent
                if( _matchLocal.ListHolderUserName == _userLocal.UserName && !_userLocal.sentUp && !_userLocal.sentDown )
                {
                    _viewListButton.SetActive(true);
                }
                else
                {
                    _viewListButton.SetActive(false);
                }
            }
        });

        UsersLocalUpdate(); // Update the local user info from Database

        _roundCurrent = (Rounds)_matchLocal.RoundCurrent; // Update the last Round Enum
        _roundChangeTime = _matchLocal.RoundTimer;

        switch (_roundCurrent)
        {
            case Rounds.Lobby:
                if (_roundCurrent != _roundLast) // Just started the round
                {
                    Debug.Log("Lobby Round has started");
                    //_matchLocal.RoundTimer = TimeUtils.GetUnixTime() + 90f;
                    //TO-DO: Update round time on database
                }
                break;

                /*
            case Rounds.Initializing:
                //if (TimeUtils.GetUnixTime() > _matchLocal.RoundTimer)
                if (_roundCurrent != _roundLast) // Just started the round
                {
                    Debug.Log("Initializing Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting starts in:";
                    //_matchLocal.RoundTimer = TimeUtils.GetUnixTime() + 90f;
                }
                break;
                */

            case Rounds.Voting:
                if (_roundCurrent != _roundLast) // Just started the round
                {
                    // check new list owner?

                    Debug.Log("Voting Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting ends in:";
                }
                break;

            case Rounds.Waiting:
                if (_roundCurrent != _roundLast) // Just started the round
                {
                    Debug.Log("Waiting Round has started");
                    _roundTitle.GetComponent<Text>().text = "Voting starts in:";
                }
                break;

            case Rounds.GameOver:
                if (_roundCurrent != _roundLast) // Just started the round
                {
                    Debug.Log("Game over Round has started");
                }
                break;
        }

        _roundLast = (Rounds)_matchLocal.RoundCurrent; // Update the last Round Enum
    }

    public void AvatarSendCheck ()
    {
        if( _userLocal.sentUp || _userLocal.sentDown )
        {
            Debug.Log("Cannot send when you are already sent!");
            return;
        }    

        User userToSend = null;
        string typedName = GameObject.Find("Player True Name InputField").GetComponent<Text>().text;

        foreach (User user in _userList)
        {
            if (user.SecretName == typedName)
            {
                userToSend = user;
                break;
            }
        }

        if (userToSend == null)
        {
            Debug.Log("Invalid Name");
            //TO-DO: add invalid name message
            return;
        }

        if (_userLocal.Team == "Ascend Team")
        {
            userToSend.sentUp = true;
        }
        else
        {
            userToSend.sentDown = true;
        }

        //-------------------Add User to database--------------------
        string json = JsonUtility.ToJson(userToSend); //Comvert class to json file
        _reference.Child("User").Child(userToSend.UserName).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully added data to Firebase.");
            }
            else
            {
                Debug.Log("Adding data failed.");
            }
        });
    }

    private void UsersLocalUpdate ()
    {
        _reference.Child("User").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                //_userListLocal.Clear(); //Clear the list before rebuilding

                DataSnapshot snapshot = task.Result;
                //Debug.Log(snapshot.Child("UserName").Value.ToString

                int count = 0;
                foreach (var child in snapshot.Children)
                {
                    count++;

                    User user = JsonUtility.FromJson<User>(snapshot.GetRawJsonValue());
                    foreach (User userCurrent in _userList)
                    {
                        if (userCurrent.UserName == user.UserName)
                        {
                            if (userCurrent.sentUp != user.sentUp || userCurrent.sentDown != user.sentDown)
                            {
                                PlayerSentPop(user);
                                if (user.UserName == _userLocal.UserName)
                                {
                                    _userLocal.sentDown = user.sentDown;
                                    _userLocal.sentUp = user.sentUp;
                                }
                            }
                        }
                        _userList.Remove(userCurrent);
                        _userList.Add(user);
                    }
                }
                //Debug.Log("number of users " + count);
            }
        });
    }

    private void PlayerSentPop (User userSent)
    {
        // Activate pop up
        playerSentPopup.SetActive(true);
        playerSentText.GetComponent<Text>().text = userSent.UserName + " was sent "+(userSent.sentUp?"up":"down")+"!";
    }

    private void GameOverCheck ()
    {
        int totalSentUp = 0;
        int totalSentDown = 0;
        int target = (_userList.Count / 2) + 1;
        foreach (User user in _userList)
        {
            if (user.sentUp == true)
            {
                totalSentUp++;
            }
            if (user.sentDown == true)
            {
                totalSentDown++;
            }

            if (totalSentUp >= target)
            {
                WinSreen(true);
                break;
            }
            if (totalSentDown >= target)
            {
                WinSreen(false);
                break;
            }
        }
    }

    private void WinSreen (bool upWon)
    {
        if( upWon)
        {
            _gameOverScreen.SetActive(true);
            _gameOverWinnerText.GetComponent<Text>().text = upWon ? "Ascend Team won!" : "Descend Team won!"; 
        }
    }

    public void PrepareForMatch ()
    {
        Match match = new Match();
        _matchLocal = match;
        _matchLocal.IsGameOver = false;
        _matchStartedClient = false;
        _matchStartedHost = false;
        _matchLocal.RoundCurrent = 0;
        //_matchInitialized = false;
        _matchLocal.AvatarsPicked.Clear(); //Clear avatar pick list
        _matchLocal.SecretNamesPicked.Clear();
        _matchLocal.SecretNamesPicked.Add("Init");
    }

    public void ResetMatch()
    {
        Match match = new Match();
        _matchLocal = match;
        match.IsGameOver = false;
        _matchStartedClient = false;
        _matchStartedHost = false;
        match.RoundCurrent = 0;
        //_matchInitialized = false;
        match.AvatarsPicked.Clear(); //Clear avatar pick list
        match.SecretNamesPicked.Clear();
        match.SecretNamesPicked.Add("Init");

        // Team Actions
        int actionAscendInt = Random.Range(0, _secretActionReference.Count - 1);
        int actionDescendInt = Random.Range(actionAscendInt + 1, _secretActionReference.Count - 2);
        match.ActionAscend = _secretActionReference[actionAscendInt];
        match.ActionDescend = _secretActionReference[actionDescendInt];

        // Save to Database
        string json = JsonUtility.ToJson(match);
        Debug.Log(json);

        _reference.Child("Match").SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully added data to Firebase.");
            }
            else
            {
                Debug.Log("Adding data failed.");
            }
        });


    }
}