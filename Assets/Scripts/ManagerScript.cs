
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionLibrary;
using Assets.Scripts;
using Assets.Scripts.Animation;
using UnityEngine;
using IntegratedAuthoringTool;
using IntegratedAuthoringTool.DTOs;
using RolePlayCharacter;
using UnityEngine.UI;
using Utilities;
using WellFormedNames;
using WorldModel;
using System.IO;
using GAIPS.Rage;

public class ManagerScript : MonoBehaviour
{

    // Store the iat file
    private IntegratedAuthoringToolAsset _iat;

    //Store the characters
    private List<RolePlayCharacterAsset> _rpcList;

    //Store the World Model
    private WorldModelAsset _worldModel;

    public Button DialogueButtonPrefab;

    private RolePlayCharacterAsset _playerRpc;

    private bool _waitingForPlayer = false;

    private List<Button> _mButtonList = new List<Button>();

    public UnityBodyImplement _agentBodyController;


    // Use this for initialization
    void Start()
    {

        // Loading Storage json with the Rules, files must be in the Streaming Assets Folder

        var storagePath = Application.streamingAssetsPath + "/John/storage.json";

        // Making sure it works on Android and Web-GL
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(storagePath);
        www.SendWebRequest();

        while(!www.isDone)
        {

        }

        String jsonString = www.downloadHandler.text;
        var storage = AssetStorage.FromJson(jsonString);
        
     
        //Loading Scenario information with data regarding characters and dialogue
        var iatPath = Application.streamingAssetsPath + "/John/couns.json";

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

       _playerRpc = _rpcList.Find(x => x.CharacterName.ToString().Contains("Player"));
        _playerRpc.IsPlayer = true;
    }



    // Update is called once per frame
    void Update()
    {
        if (_waitingForPlayer) 
            return;

        if( _agentBodyController._speechController.IsPlaying)
            return;


        IAction finalDecision = null;
        String initiatorAgent = "";

        // A simple cycle to go through all the agents and get their decision (for now there is only the Player and Charlie)
        foreach (var rpc in _rpcList)
        {

            // From all the decisions the rpc wants to perform we want the first one (as it is ordered by priority)
            var decision = rpc.Decide().FirstOrDefault();

            if (rpc.IsPlayer)
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
                    " " + initiatorAgent + " decided to " + decision.Name.ToString();
                break;
            }

        }


        if (finalDecision != null)

        {
           ChooseDialogue(finalDecision, (Name)initiatorAgent);
        }


        // We can update the Character's Facial Expression each frame to make sure their avatars show emotion
        UpdateAgentFacialExpression();
    }


    void Reply(System.Guid id, Name initiator, Name target)

    {
        Debug.Log("We are replying" + initiator + " and "  + target);
        // Retrieving the chosen dialog object
        var dialog = _iat.GetDialogActionById(id);

        // Playing the audio of the dialogue line
       this.StartCoroutine(Speak(id, initiator, target));


        //Writing the dialog on the canvas
        GameObject.Find("DialogueText").GetComponent<Text>().text =
            initiator + ":  " + dialog.Utterance;


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
        Debug.Log(" The agent " + initiator + " decided to perform " + action.Name);
        
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

        if(decision !=null)
        if (decision.Key.ToString() == "Speak")
        {
            //                                          NTerm: 0     1     2     3     4
            // If it is a speaking action it is composed by Speak ( [ms], [ns] , [m}, [sty])
            var currentState = decision.Name.GetNTerm(1);
            var nextState = decision.Name.GetNTerm(2);
            var meaning = decision.Name.GetNTerm(3);
            var style = decision.Name.GetNTerm(4);


            // Returns a list of all the dialogues given the parameters
            var dialog = _iat.GetDialogueActions(currentState, (Name)"*", (Name)"*", (Name)"*");
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
        //Get the strongest emotion felt by the RPC that is not the Player
      var strongestEmotion =  _rpcList.Find(x => x.IsPlayer == false).GetStrongestActiveEmotion();

      if(_agentBodyController !=null && strongestEmotion != null)
        _agentBodyController.SetExpression(strongestEmotion.EmotionType, strongestEmotion.Intensity/10);
    

    }


    // Method to play the audio file of the specific dialogue, aka what makes the agent talk
    private IEnumerator Speak(System.Guid id, Name initiator, Name target)
    {

        // The player has no body, so we use a shortcut and ignore him having a voice at all
        if(_playerRpc.CharacterName == initiator)
            yield break;

        // What is the type of of Voice of the agent
        var voiceType = _rpcList.Find(x => x.CharacterName == initiator).VoiceName;

        // Each utterance has a unique Id so we can retrieve its audio file
        var utteranceID = _iat.GetDialogActionById(id).UtteranceId;

        // This path can be changed, for now it is the path we used in this project
        var textToSpeechPath = "/SingleCharacterv4.0/TTS/" + voiceType + "/" + utteranceID;

        var absolutePath = Application.streamingAssetsPath;
        

#if UNITY_EDITOR || UNITY_STANDALONE
        absolutePath = "file://" + absolutePath;
#endif
       
        // Systems tried to "download" the .wav file along with its xml configuration
        string audioUrl = absolutePath + textToSpeechPath + ".wav";
        string xmlUrl = absolutePath + textToSpeechPath + ".xml";

        var audio = new WWW(audioUrl);
        var xml = new WWW(xmlUrl);

        yield return audio;
        yield return xml;

        // If these files were not found there return
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
           yield return _agentBodyController.PlaySpeech(clip, xml.text);

            clip.UnloadAudioData();
        }
    }
}
