
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

#if UNITY_EDITOR && UNITY_WEBGL

        Debug.Log("Web-GL");

        String storageString = "[\"EmotionalAppraisalAsset\",{\"root\":{\"classId\":0,\"Description\":null,\"AppraisalRules\":{\"AppraisalWeight\":1,\"Rules\":[{\"EventName\":\"Event(Action-End, SELF, Speak([cs], [ns], *, *), John)\",\"Conditions\":{\"Set\":[]},\"AppraisalVariables\":{\"AppraisalVariables\":[{\"Name\":\"Praiseworthiness\",\"Value\":-5,\"Target\":\"SELF\"}]}},{\"EventName\":\"Event(Action-End, SELF, Speak([cs], [ns], *, *), Charlie)\",\"Conditions\":{\"Set\":[]},\"AppraisalVariables\":{\"AppraisalVariables\":[{\"Name\":\"Praiseworthiness\",\"Value\":-5,\"Target\":\"SELF\"}]}},{\"EventName\":\"Event(Action-End, SELF, Do, John)\",\"Conditions\":{\"Set\":[]},\"AppraisalVariables\":{\"AppraisalVariables\":[{\"Name\":\"Praiseworthiness\",\"Value\":-5,\"Target\":\"John\"}]}},{\"EventName\":\"Event(Action-End, SELF, Do, Charlie)\",\"Conditions\":{\"Set\":[]},\"AppraisalVariables\":{\"AppraisalVariables\":[{\"Name\":\"Praiseworthiness\",\"Value\":-6,\"Target\":\"Chalie\"}]}},{\"EventName\":\"Event(Property-Change, SELF, Hello(World), [t])\",\"Conditions\":{\"Set\":[]},\"AppraisalVariables\":{\"AppraisalVariables\":[{\"Name\":\"Praiseworthiness\",\"Value\":6,\"Target\":\"[t]\"}]}}]}},\"types\":[{\"TypeId\":0,\"ClassName\":\"EmotionalAppraisal.EmotionalAppraisalAsset, EmotionalAppraisal, Version=1.4.1.0, Culture=neutral, PublicKeyToken=null\"}]},\"EmotionalDecisionMakingAsset\",{\"root\":{\"classId\":0,\"ActionTendencies\":[{\"Action\":\"Speak([cs], [ns], [mean], [style])\",\"Target\":\"[t]\",\"Layer\":\"-\",\"Conditions\":{\"Set\":[\"DialogueState([t]) = [cs]\",\"Has(Floor) = SELF\",\"ValidDialogue([cs], [ns], [mean], [style]) = True\"]},\"Priority\":1},{\"Action\":\"Speak([cs], [ns], [mean], Rude)\",\"Target\":\"[t]\",\"Layer\":\"-\",\"Conditions\":{\"Set\":[\"DialogueState([t]) = [cs]\",\"ValidDialogue([cs], [ns], [mean], Rude) = True\",\"Has(Floor) = SELF\",\"Mood(SELF) < 0\"]},\"Priority\":5},{\"Action\":\"Speak([cs], [ns], [mean], Polite)\",\"Target\":\"[t]\",\"Layer\":\"-\",\"Conditions\":{\"Set\":[\"DialogueState([t]) = [cs]\",\"ValidDialogue([cs], [ns], [mean], Polite) = True\",\"Has(Floor) = SELF\",\"Mood(SELF) < 0\"]},\"Priority\":5}]},\"types\":[{\"TypeId\":0,\"ClassName\":\"EmotionalDecisionMaking.EmotionalDecisionMakingAsset, EmotionalDecisionMaking, Version=1.2.0.0, Culture=neutral, PublicKeyToken=null\"}]},\"SocialImportanceAsset\",{\"root\":{\"classId\":0,\"AttributionRules\":[{\"RuleName\":\"Good Mood\",\"Target\":\"[t]\",\"Value\":10,\"Conditions\":{\"Set\":[\"Mood(SELF) > 0\"]}},{\"RuleName\":\"Close Friends\",\"Target\":\"[t]\",\"Value\":20,\"Conditions\":{\"Set\":[\"CloseFriends([t]) = True\"]}},{\"RuleName\":\"TalktTo\",\"Target\":\"[t]\",\"Value\":40,\"Conditions\":{\"Set\":[\"EventId(Action-End, [t], Speak(*, *, *, *), SELF) != -1\"]}}]},\"types\":[{\"TypeId\":0,\"ClassName\":\"SocialImportance.SocialImportanceAsset, SocialImportance, Version=1.5.0.0, Culture=neutral, PublicKeyToken=null\"}]},\"CommeillFautAsset\",{\"root\":{\"classId\":0,\"SocialExchanges\":[]},\"types\":[{\"TypeId\":0,\"ClassName\":\"CommeillFaut.CommeillFautAsset, CommeillFaut, Version=1.7.0.0, Culture=neutral, PublicKeyToken=null\"}]}]";

        String scenarioString = "{\"root\":{\"classId\":0,\"ScenarioName\":\"Example\",\"Description\":\"A short conversation between the Player and a Character named Charlie. Charlie discovers that there is a major conspiracy within the company he works in. \",\"Dialogues\":[{\"CurrentState\":\"Leave\",\"NextState\":\"End\",\"Meaning\":\"-\",\"Style\":\"-\",\"Utterance\":\"Alright, goodbye\",\"UtteranceId\":\"TTS-CCF7877090E49659FB15B89D80C365A7\"},{\"CurrentState\":\"Greeting\",\"NextState\":\"Order\",\"Meaning\":\"-\",\"Style\":\"Polite\",\"Utterance\":\"How do you do?\",\"UtteranceId\":\"TTS-E191BADF9DAD8B0EDC942E4A6DFC8D64\"},{\"CurrentState\":\"Greeting\",\"NextState\":\"Leave\",\"Meaning\":\"-\",\"Style\":\"VeryRude\",\"Utterance\":\"Not you again\",\"UtteranceId\":\"TTS-4DDAA9A4B302A0E4E9DEE292CDB9481D\"},{\"CurrentState\":\"Order\",\"NextState\":\"OrderResponse\",\"Meaning\":\"Hamburger\",\"Style\":\"-\",\"Utterance\":\"Yes, I would like a burger please\",\"UtteranceId\":\"TTS-6D501E2FE494013994B75A7225E5FA29\"},{\"CurrentState\":\"Greeting\",\"NextState\":\"Order\",\"Meaning\":\"-\",\"Style\":\"Polite\",\"Utterance\":\"How can I help you?\",\"UtteranceId\":\"TTS-C425F01490F94A3080B9922108A78C33\"},{\"CurrentState\":\"Start\",\"NextState\":\"Greeting\",\"Meaning\":\"-\",\"Style\":\"Rude\",\"Utterance\":\"Hey\",\"UtteranceId\":\"TTS-6057F13C496ECF7FD777CEB9E79AE285\"},{\"CurrentState\":\"Start\",\"NextState\":\"Greeting\",\"Meaning\":\"-\",\"Style\":\"Polite\",\"Utterance\":\"Good Afternoon\",\"UtteranceId\":\"TTS-91145E15F72DF3A48A9E83CAE7E3BED7\"},{\"CurrentState\":\"Order\",\"NextState\":\"OrderResponse\",\"Meaning\":\"Pizza\",\"Style\":\"-\",\"Utterance\":\"Yes, I would like a Pizza please\",\"UtteranceId\":\"TTS-E2A8BDFAB9C5D5B8A5CD9A67F8C08155\"}],\"Characters\":[{\"KnowledgeBase\":{\"Perspective\":\"Charlie\",\"Knowledge\":{\"SELF\":{\"Has(Floor)\":\"Charlie, 1\",\"DialogueState(Player)\":\"Start, 1\",\"AM(Charlie)\":\"True, 1\",\"CloseFriends(Player)\":\"False, 1\"}}},\"BodyName\":\"Male\",\"VoiceName\":\"Male\",\"EmotionalState\":{\"Mood\":4,\"initialTick\":0,\"EmotionalPool\":[],\"AppraisalConfiguration\":{\"HalfLifeDecayConstant\":0.5,\"EmotionInfluenceOnMoodFactor\":0.3,\"MoodInfluenceOnEmotionFactor\":0.3,\"MinimumMoodValueForInfluencingEmotions\":0.5,\"EmotionalHalfLifeDecayTime\":15,\"MoodHalfLifeDecayTime\":60}},\"AutobiographicMemory\":{\"Tick\":0,\"records\":[]},\"OtherAgents\":{\"dictionary\":[]},\"Goals\":[{\"Name\":\"Survive\",\"Significance\":5,\"Likelihood\":0.5}]},{\"KnowledgeBase\":{\"Perspective\":\"Player\",\"Knowledge\":{\"SELF\":{\"Has(Floor)\":\"Charlie, 1\",\"DialogueState(Charlie)\":\"Start, 1\"}}},\"BodyName\":null,\"VoiceName\":null,\"EmotionalState\":{\"Mood\":-3,\"initialTick\":0,\"EmotionalPool\":[],\"AppraisalConfiguration\":{\"HalfLifeDecayConstant\":0.5,\"EmotionInfluenceOnMoodFactor\":0.3,\"MoodInfluenceOnEmotionFactor\":0.3,\"MinimumMoodValueForInfluencingEmotions\":0.5,\"EmotionalHalfLifeDecayTime\":15,\"MoodHalfLifeDecayTime\":60}},\"AutobiographicMemory\":{\"Tick\":0,\"records\":[]},\"OtherAgents\":{\"dictionary\":[]},\"Goals\":[{\"Name\":\"Survive\",\"Significance\":5,\"Likelihood\":0.2}]}],\"WorldModel\":{\"Effects\":{\"dictionary\":[{\"key\":\"Event(Action-End, [s], Speak(*, [ns], *, *), [t])\",\"value\":[{\"PropertyName\":\"DialogueState([s])\",\"NewValue\":\"[ns]\",\"ObserverAgent\":\"[t]\"},{\"PropertyName\":\"Has(Floor)\",\"NewValue\":\"[t]\",\"ObserverAgent\":\"*\"},{\"PropertyName\":\"DialogueState([s])\",\"NewValue\":\"[ns]\",\"ObserverAgent\":\"Player\"}]}]},\"Priorities\":{\"dictionary\":[{\"key\":\"Event(Action-End, [s], Speak(*, [ns], *, *), [t])\",\"value\":1}]}}},\"types\":[{\"TypeId\":0,\"ClassName\":\"IntegratedAuthoringTool.IntegratedAuthoringToolAsset, IntegratedAuthoringTool, Version=1.7.0.0, Culture=neutral, PublicKeyToken=null\"}]}";

#else

        // Loading Storage json with the Rules, files must be in the Streaming Assets Folder

        var storagePath = Application.streamingAssetsPath + "/SingleCharacter/storage.json";

        // Making sure it works on Android and Web-GL
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(storagePath);
        www.SendWebRequest();

        while(!www.isDone)
        {

        }

        String storageString = www.downloadHandler.text;
        
     
        //Loading Scenario information with data regarding characters and dialogue
        var iatPath = Application.streamingAssetsPath + "/SingleCharacter/scenario.json";

        //I have to do the same I just did before
        // Making sure it works on Android and Web-GL
        www = UnityEngine.Networking.UnityWebRequest.Get(iatPath);
        www.SendWebRequest();

        while (!www.isDone)
        {

        }

        String scenarioString = www.downloadHandler.text;
#endif

            var storage = AssetStorage.FromJson(storageString);
        // Now that I have gotten the string for sure I can load the IAT
        _iat = IntegratedAuthoringToolAsset.FromJson(scenarioString, storage);

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
            var dialog = _iat.GetDialogueActions(currentState, nextState, (Name)"*", (Name)"*");
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
        var textToSpeechPath = "/SingleCharacter/TTS/" + voiceType + "/" + utteranceID;

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
