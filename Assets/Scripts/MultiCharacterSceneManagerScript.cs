
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



public class MultiCharacterSceneManagerScript : MonoBehaviour
{

    // Store the iat file
    private IntegratedAuthoringToolAsset _iat;

    public String IatPath;

    //Store the characters
    private List<RolePlayCharacterAsset> _rpcList;

    //Store the World Model
    private WorldModelAsset _worldModel;

    public Button DialogueButtonPrefab;

    private RolePlayCharacterAsset _playerRpc;

    private bool _waitingForPlayer = false;

    private List<Button> _mButtonList = new List<Button>();

    private List<UnityBodyImplement> _agentBodyControlers;

    //Time given to each character's dialogue in case there is no text to speech
    public float dialogueTimer;
    //Auxiliary variable
    private float dialogueTimerAux;

    // Used canvas
    public Canvas initialCanvas;
    public Canvas GameCanvas;

    // Choose your character button prefab
    public Button menuButtonPrefab;

    
    // Auxiliary Variables
    private bool initialized = false;

    // If there is no text to speech leave at false

    public bool useTextToSpeech;


    public List<GameObject> CharacterBodies;

 
    // Use this for initialization
    void Start()
    {
        // Loading Storage json with the Rules, files must be in the Streaming Assets Folder
        var storagePath = Application.streamingAssetsPath + "/MultiCharacter/multicharstorage.json";

        // Making sure it works on Android and Web-GL
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(storagePath);
        www.SendWebRequest();

        while (!www.isDone)
        {

        }

        String jsonString = www.downloadHandler.text;
        var storage = AssetStorage.FromJson(jsonString);

        //Loading Scenario information with data regarding characters and dialogue
        var iatPath = Application.streamingAssetsPath + "/MultiCharacter/scenario.json";

        //I have to do the same I just did before
        // Making sure it works on Android and Web-GL
        www = UnityEngine.Networking.UnityWebRequest.Get(iatPath);
        www.SendWebRequest();

        while (!www.isDone)
        {

        }

        jsonString = www.downloadHandler.text;

        // Now that I have gotten the string for sure I can load the IAT
        _iat = IntegratedAuthoringToolAsset.FromJson(jsonString, storage);


        var currentState = IATConsts.INITIAL_DIALOGUE_STATE;


        // Getting a list of all the Characters
        _rpcList = _iat.Characters.ToList();

        //Saving the World Model
        _worldModel = _iat.WorldModel;



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

        Debug.Log("Player chose " + rpc.CharacterName + " number of rpcs " + _rpcList.Count);
      
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

        // Initializing textual information of first character
        var firstCharacterName = otherRPCsList.FirstOrDefault().CharacterName.ToString();

        CharacterBodies.FirstOrDefault().GetComponentInChildren<TextMesh>().text = firstCharacterName;

        //Initializing and saving into a list the Body Controller of the First Character
        var unityBodyImplement = CharacterBodies.FirstOrDefault().GetComponent<UnityBodyImplement>();
        unityBodyImplement.gameObject.tag = firstCharacterName;

        _agentBodyControlers.Add(unityBodyImplement);


        // Initializing textual information of second character
        var secondCharacterName = otherRPCsList[otherRPCsList.Count - 1].CharacterName.ToString();

        CharacterBodies[CharacterBodies.Count - 1].GetComponentInChildren<TextMesh>().text = secondCharacterName;

        //Initializing and saving into a list the Body Controller of the second Character
        unityBodyImplement = CharacterBodies[CharacterBodies.Count - 1].GetComponentInChildren<UnityBodyImplement>();
        unityBodyImplement.gameObject.tag = secondCharacterName;

        _agentBodyControlers.Add(unityBodyImplement);

        foreach(var e in _agentBodyControlers)
        {
            Debug.Log(e.Body);
        }

        // This sequence could've been much more generic, I'm just lazy
        otherRPCsList.Add(rpc);
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
        if (!initialized) return;


        if (_waitingForPlayer) 
            return;

        if(_agentBodyControlers.Any(x=>x._speechController.IsPlaying))
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
                continue;;

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
        dialogueTimerAux =  dialogueTimer;
        // Retrieving the chosen dialog object
        var dialog = _iat.GetDialogActionById(id);

        // Playing the audio of the dialogue line

        if(useTextToSpeech)
       this.StartCoroutine(Speak(id, initiator, target));


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

        _rpcList.Find(x=>x.CharacterName == initiator).Perceive(eventName);
        _rpcList.Find(x=>x.CharacterName == target).Perceive(eventName);

        //Handle the consequences of their actions
        HandleEffects(eventName);
    }


    void HandleEffects(Name _event)
    {
        var consequences = _worldModel.Simulate(new Name[] {_event} );

        // For each effect 
        foreach (var eff in consequences)
        {
            Debug.Log("Effect: " + eff.PropertyName + " " + eff.NewValue + " " + eff.ObserverAgent);

            // For each Role Play Character
            foreach (var rpc in _rpcList)
            {

                //If the "Observer" part of the effect corresponds to the name of the agent or if it is a universal symbol
                if (eff.ObserverAgent != rpc.CharacterName && eff.ObserverAgent != (Name) "*") continue;
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

        
        if(dialog!=null)
            Reply(dialog.Id, initiator, action.Target);
    }


    void HandlePlayerOptions(IAction decision)
    {
        _waitingForPlayer = true;
        if(decision != null)
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

            foreach(var d in dialog)
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
            
            b.GetComponentInChildren<Text>().text = i + ": " +  d.Utterance;
            
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
      foreach(var agent in _agentBodyControlers)
        {
            var strongestEmotion = _rpcList.Find(x => x.CharacterName.ToString() == agent.gameObject.tag).GetStrongestActiveEmotion();

            if (strongestEmotion != null)
            agent.SetExpression(strongestEmotion.EmotionType, strongestEmotion.Intensity / 10);
        }
    

    }


    // Method to play the audio file of the specific dialogue, aka what makes the agent talk
    private IEnumerator Speak(System.Guid id, Name initiator, Name target)
    {

        // The player has no body, as a consequence there is no reason for him to speak
        if(_playerRpc.CharacterName == initiator)
            yield break;

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

        var audio = new WWW(audioUrl);
        var xml = new WWW(xmlUrl);

        yield return audio;
        yield return xml;

        // If these files were not found then simply return
        var xmlError = !string.IsNullOrEmpty(xml.error);
        var audioError = !string.IsNullOrEmpty(audio.error);

        if (xmlError)
            Debug.LogError(xml.error);
        if (audioError)
            Debug.LogError(audio.error);

        if (xmlError || audioError)
        {
            yield return new WaitForSeconds(2);

        }

        else
        {

            var clip = audio.GetAudioClip(false);

            // The Unity Body Implement script allows us to play sound clips
            var initiatorBodyController = _agentBodyControlers.Find(x => x.gameObject.tag == initiator.ToString());
            yield return initiatorBodyController.PlaySpeech(clip, xml.text);

            clip.UnloadAudioData();
        }
    }
}
