
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionLibrary;
using Assets.Scripts;
using Assets.Scripts.Animation;
using UnityEngine;
using System.IO;
using IntegratedAuthoringTool;
using IntegratedAuthoringTool.DTOs;
using RolePlayCharacter;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities;
using WellFormedNames;
using WorldModel;
using GAIPS.Rage;
using System.Net;
using UnityEngine.Networking;


public class ManagerScript : MonoBehaviour
{

    // Store the iat file
    private IntegratedAuthoringToolAsset _iat;

    [Header("Folder and File Names")]
    public string rootFolder;
    public string scenarioName;
    public string storageName;

    //Store the characters
    private List<RolePlayCharacterAsset> _rpcList;
    private AssetStorage storage;

    //Store the World Model
    private WorldModelAsset _worldModel;

    [Header("Prefabs")]
    public Button DialogueButtonPrefab;

    private RolePlayCharacterAsset _playerRpc;

    private bool _waitingForPlayer = false;

    private List<Button> _mButtonList = new List<Button>();

    private List<UnityBodyImplement> _agentBodyControlers;

    // We need to save information returned by UnityWebRequest during the loading process
    string scenarioInfo = "";
    string storageInfo = "";
    bool scenarioDone = false;
    bool storageDone = false;

   

    //Dealing with Audio and XML relevant for Web-GL
    UnityWebRequest audio;
    UnityWebRequest xml;
    string initiator;
    bool audioReady = false;
    bool xmlReady = false;
    bool audioNeeded = false;

    // Used canvas
    public Canvas initialCanvas;
    public Canvas GameCanvas;

    // Choose your character button prefab
    public Button menuButtonPrefab;


    // Auxiliary Variables
    private bool initialized = false;

    //Time given to each character's dialogue in case there is no text to speech
    public float dialogueTimer;
    //Auxiliary variable
    private float dialogueTimerAux;

    // If there is no text to speech leave at false
    public bool useTextToSpeech;

    // Different models available to agents
    public List<GameObject> CharacterBodies;

    private bool isReady = false;
    private Dictionary<string, GameObject> nameToBody;

    // Use this for initialization
    void Start()
    {
        Debug.Log("Loading...");
        var streamingAssetsPath = Application.streamingAssetsPath;
#if UNITY_EDITOR || UNITY_STANDALONE

        streamingAssetsPath = "file://" + streamingAssetsPath;
#endif
        nameToBody = new Dictionary<string, GameObject>();
        // Loading Storage json with the Rules, files must be in the Streaming Assets Folder
        var storagePath = streamingAssetsPath + "/" + rootFolder + "/" + storageName + ".json";

        //Loading Scenario information with data regarding characters and dialogue
        var iatPath = streamingAssetsPath + "/" + rootFolder + "/" + scenarioName + ".json";

        StartCoroutine(GetStorage(storagePath));

        StartCoroutine(GetScenario(iatPath));

    }


    void LoadedScenario()
    {
        var currentState = IATConsts.INITIAL_DIALOGUE_STATE;

        // Getting a list of all the Characters
        _rpcList = _iat.Characters.ToList();

        //Saving the World Model
        _worldModel = _iat.WorldModel;
 
        Debug.Log("Loading has finished");

        isReady = true;
        ChooseCharacterMenu();
    }


    void ChooseCharacterMenu()
    {

        foreach (var rpc in _rpcList)
        {

            // What happens when the player chooses to be a particular character

            AddButton(rpc.CharacterName.ToString(), () =>
            {
                LoadGame(rpc);

            });
        }


    }



    private void LoadGame(RolePlayCharacterAsset rpc)
    {

        _playerRpc = rpc;

        _playerRpc.IsPlayer = true;
        // Turn off the choose your character panel
        initialCanvas.gameObject.SetActive(false);
        //Turning on the Dialogue canvas
        GameCanvas.gameObject.SetActive(true);

        // Update character's name in the Game although I'm overcomplicating things a bit.
        var otherRPCsList = _rpcList;
        otherRPCsList.Remove(rpc);

        _agentBodyControlers = new List<UnityBodyImplement>();

        foreach (var agent in otherRPCsList)
        {
            // Initializing textual for each different character
            var charName = agent.CharacterName.ToString();
            var rand = UnityEngine.Random.Range(0, CharacterBodies.Count);
            nameToBody.Add(charName, CharacterBodies[rand]);
            CharacterBodies.RemoveAt(rand);
            var body = nameToBody[charName];
            //Initializing and saving into a list the Body Controller of the First Character
            var unityBodyImplement = body.GetComponent<UnityBodyImplement>();
            body.name  = charName;
            body.GetComponentInChildren<TextMesh>().text = charName;

            _agentBodyControlers.Add(unityBodyImplement);

        }
        _rpcList.Add(_playerRpc);
        initialized = true;
    }

    // Instantiating the chose your character buttons
    private void AddButton(string label, UnityAction action)
    {

        var parent = GameObject.Find("ChooseCharacter");

        var button = Instantiate(menuButtonPrefab, parent.transform);

        var buttonLabel = button.GetComponentInChildren<Text>();
        buttonLabel.text = label;

        button.onClick.AddListener(action);
    }


    // Update is called once per frame
    void Update()
    {
        if (!isReady)
        {
            if(scenarioDone && storageDone)
            {
                Debug.Log("Finished Reading Files");
                isReady = true;
                LoadWebGL();
            }
        }

        
        if (!initialized) return;


        if (_agentBodyControlers.Any(x => x._speechController.IsPlaying))
            return;

        if (audioNeeded)
        {
           
            if (audioReady && xmlReady)
                StartCoroutine(PlayAudio());
            else return;
        }

        if (_waitingForPlayer)
            return;



        if (dialogueTimerAux > 0)
        {
            dialogueTimerAux -= Time.deltaTime;
            return;
        }


        IAction finalDecision = null;
        String initiatorAgent = "";

        // A simple cycle to go through all the agents and get their decision (for now there is only the Player and Charlie)
        foreach (var rpc in _rpcList)
        {

            // From all the decisions the rpc wants to perform we want the first one (as it is ordered by priority)
            var decision = rpc.Decide().FirstOrDefault();



            if (_playerRpc.CharacterName == rpc.CharacterName)
            {
                HandlePlayerOptions(decision);
                continue; ;

            }

            if (decision != null)
            {

                initiatorAgent = rpc.CharacterName.ToString();
                finalDecision = decision;


                //Write the decision on the canvas
                GameObject.Find("DecisionText").GetComponent<Text>().text =
                    " " + initiatorAgent + " decided to " + decision.Name.ToString() + " towards " + decision.Target;
                break;
            }

        }


        if (finalDecision != null)

        {
            ChooseDialogue(finalDecision, (Name)initiatorAgent);
        }


        // We can update the Facial Expression each frame to keep believability
        UpdateAgentFacialExpression();
    }


    void Reply(System.Guid id, Name initiator, Name target)

    {
        dialogueTimerAux = dialogueTimer;
        // Retrieving the chosen dialog object
        var dialog = _iat.GetDialogActionById(id);

        // Playing the audio of the dialogue line

        if (useTextToSpeech)
        {
            this.StartCoroutine(Speak(id, initiator, target));
        }



        //Writing the dialog on the canvas
        GameObject.Find("DialogueText").GetComponent<Text>().text =
            initiator + " says:  '" + dialog.Utterance + "' ->towards " + target;


        // Getting the full action Name
        var actualActionName = "Speak(" + dialog.CurrentState + ", " + dialog.NextState + ", " + dialog.Meaning +
                               ", " + dialog.Meaning + ")";

        //So we can generate its event
        var eventName = EventHelper.ActionEnd(initiator, (Name)actualActionName, target);


        ClearAllDialogButtons();

        //Inform each participating agent of what happened

        _rpcList.Find(x => x.CharacterName == initiator).Perceive(eventName);
        _rpcList.Find(x => x.CharacterName == target).Perceive(eventName);

        //Handle the consequences of their actions
        HandleEffects(eventName);
    }


    void HandleEffects(Name _event)
    {
        var consequences = _worldModel.Simulate(new Name[] { _event });

        // For each effect 
        foreach (var eff in consequences)
        {
            Debug.Log("Effect: " + eff.PropertyName + " " + eff.NewValue + " " + eff.ObserverAgent);

            // For each Role Play Character
            foreach (var rpc in _rpcList)
            {

                //If the "Observer" part of the effect corresponds to the name of the agent or if it is a universal symbol
                if (eff.ObserverAgent != rpc.CharacterName && eff.ObserverAgent != (Name)"*") continue;
                //Apply that consequence to the agent
                rpc.Perceive(EventHelper.PropertyChange(eff.PropertyName, eff.NewValue, rpc.CharacterName));

            }
        }

        _waitingForPlayer = false;
    }



    void ChooseDialogue(IAction action, Name initiator)
    {
        Debug.Log(" The agent " + initiator + " decided to perform " + action.Name + " towards " + action.Target);

        //                                          NTerm: 0     1     2     3     4
        // If it is a speaking action it is composed by Speak ( [ms], [ns] , [m}, [sty])
        var currentState = action.Name.GetNTerm(1);
        var nextState = action.Name.GetNTerm(2);
        var meaning = action.Name.GetNTerm(3);
        var style = action.Name.GetNTerm(4);


        // Returns a list of all the dialogues given the parameters but in this case we only want the first element
        var dialog = _iat.GetDialogueActions(currentState, nextState, meaning, style).FirstOrDefault();


        if (dialog != null)
            Reply(dialog.Id, initiator, action.Target);
    }


    void HandlePlayerOptions(IAction decision)
    {
        _waitingForPlayer = true;
        if (decision != null)
            if (decision.Key.ToString() == "Speak")
            {
                //                                          NTerm: 0     1     2     3     4
                // If it is a speaking action it is composed by Speak ( [ms], [ns] , [m}, [sty])
                var currentState = decision.Name.GetNTerm(1);
                var nextState = decision.Name.GetNTerm(2);
                var meaning = decision.Name.GetNTerm(3);
                var style = decision.Name.GetNTerm(4);


                // Returns a list of all the dialogues given the parameters
                var dialog = _iat.GetDialogueActions(currentState, nextState, (Name)"*", (Name)"*");

                foreach (var d in dialog)
                {
                    d.Utterance = _playerRpc.ProcessWithBeliefs(d.Utterance);
                }

                AddDialogueButtons(dialog, decision.Target);


            }

            else Debug.LogWarning("Unknown action: " + decision.Key);

    }


    // Instantiating the Dialog Button Prefab on the DialogueOptions object in the Canvas
    void AddDialogueButtons(List<DialogueStateActionDTO> dialogs, Name target)
    {

        var i = 0;
        foreach (var d in dialogs)
        {
            var b = Instantiate(DialogueButtonPrefab);

            var t = b.transform;

            t.SetParent(GameObject.Find("DialogueOptions").transform, false);

            i++;

            b.GetComponentInChildren<Text>().text = i + ": " + d.Utterance;

            var id = d.Id;

            b.onClick.AddListener(() => Reply(id, _playerRpc.CharacterName, target));

            _mButtonList.Add(b);

        }
    }


    void ClearAllDialogButtons()
    {
        foreach (var b in _mButtonList)
        {
            Destroy(b.gameObject);
        }
        _mButtonList.Clear();
    }


    void UpdateAgentFacialExpression()
    {
        foreach (var agent in _agentBodyControlers)
        {
            var strongestEmotion = _rpcList.Find(x => x.CharacterName.ToString() == agent.gameObject.name).GetStrongestActiveEmotion();

            if (strongestEmotion != null)
            {
                try
                {
                    agent.SetExpression(strongestEmotion.EmotionType, strongestEmotion.Intensity / 10);
                } catch (Exception e)
                {
                    Debug.Log("Exception Caught: " + e.Message);
                }

            }
        }


    }


    // Method to play the audio file of the specific dialogue, aka what makes the agent talk
    private IEnumerator Speak(System.Guid id, Name initiator, Name target)
    {
        
        // The player has no body, as a consequence there is no reason for him to speak
        if (_playerRpc.CharacterName == initiator)
            yield break;

      
        audioNeeded = true;
        xmlReady = false;
        audioReady = false;
        this.initiator = initiator.ToString();
        // What is the type of of Voice of the agent
        var voiceType = _rpcList.Find(x => x.CharacterName == initiator).VoiceName;

        // Each utterance has a unique Id so we can retrieve its audio file
        var utteranceID = _iat.GetDialogActionById(id).UtteranceId;

        // This path can be changed, for now it is the path we used in this project
        var textToSpeechPath = "/MultiCharacter/TTS/" + voiceType + "/" + utteranceID;

        var absolutePath = Application.streamingAssetsPath;


#if UNITY_EDITOR || UNITY_STANDALONE
        absolutePath = "file://" + absolutePath;
#endif

        // System tries to "download" the .wav file along with its xml configuration
        string audioUrl = absolutePath + textToSpeechPath + ".wav";
        string xmlUrl = absolutePath + textToSpeechPath + ".xml";

        StartCoroutine(GetXML(xmlUrl));
        StartCoroutine(GetAudioURL(audioUrl));
       


    }


    private IEnumerator PlayAudio()
    {

        Debug.Log("Playing Audio");
        if (useTextToSpeech)
        {
            var clip = DownloadHandlerAudioClip.GetContent(audio);
            // The Unity Body Implement script allows us to play sound clips
            var initiatorBodyController = _agentBodyControlers.Find(x => x.gameObject.name == initiator.ToString());
            Debug.Log("initiator:" + initiator.ToString());
            yield return initiatorBodyController.PlaySpeech(clip, xml.downloadHandler.text);

            clip.UnloadAudioData();
            audioNeeded = false;
        }
        else audioNeeded = false;

    }


    void LoadWebGL()
    {
        Debug.Log("Loading Web Gl Method");

        Debug.Log("Loading Storage string");
        storage = AssetStorage.FromJson(storageInfo);

        Debug.Log("Loading IAT string");
        _iat = IntegratedAuthoringToolAsset.FromJson(scenarioInfo, storage);

        Debug.Log("Finished Loading Web-GL");
       
        LoadedScenario();
    }


    IEnumerator GetScenario(string path)
    {
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;

            scenarioInfo = www.downloadHandler.text;
            Debug.Log("Loaded Scenario:" + scenarioInfo.ToString());
            scenarioDone = true;
        }
    }

    IEnumerator GetStorage(string path)
    {
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;

            storageInfo = www.downloadHandler.text;
            Debug.Log("Loaded Storage:" + storageInfo.ToString());
            storageDone = true;
        }

    }

    IEnumerator GetAudioURL(string path)
    {
      
        audio = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);

        yield return audio.SendWebRequest();

        if (audio.result != UnityWebRequest.Result.Success)
        {

            if (audio.result == UnityWebRequest.Result.DataProcessingError || audio.result == UnityWebRequest.Result.ConnectionError || audio.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("Audio not found error: " + audio.error);
                audioReady = true;
                useTextToSpeech = false;
                yield return null;
            }
        }
        else
        {

            audioReady = true;
        }

    }

    IEnumerator GetXML(string path)
    {
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            if (www.result == UnityWebRequest.Result.DataProcessingError || www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("XML not found error: " + www.error);
                xmlReady = true;
                useTextToSpeech = false;
                yield return null;
            }

        }
        else
        {
            xml = www;
            xmlReady = true;


        }

    }


}
