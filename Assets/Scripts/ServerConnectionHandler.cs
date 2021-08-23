using ActionLibrary;
using AutobiographicMemory.DTOs;
using CommeillFaut;
using EmotionalAppraisal;
using EmotionalDecisionMaking;
using GAIPS.Rage;
using IntegratedAuthoringTool;
using IntegratedAuthoringTool.DTOs;
using KnowledgeBase.DTOs;
using RolePlayCharacter;
using SocialImportance;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.DataStructures;
using WellFormedNames;
using System.Net.Http;
using WorldModel;
using System.Threading.Tasks;
using System.Text;
using System.Net.NetworkInformation;
using Unity;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ServerConnectionHandler : MonoBehaviour
{
    private IntegratedAuthoringToolAsset _iat;
    private AssetStorage _storage;
    private bool pinging = false;
    private bool reachedServer = false;
    private bool sendingRequest = false;
    private string _description = "";
    
    // Start is called before the first frame update
    void Start()
    {
        _iat = new IntegratedAuthoringToolAsset();
        _storage = new AssetStorage();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(pinging)
            if (reachedServer)
            {
                StopCoroutine(PingServer());
                Debug.Log("Sucessfully Reached Server");
                pinging = false;
                StartCoroutine(SendDescription());
            }
        
    }

    private static readonly HttpClient client = new HttpClient();
    // Server IP
    private static readonly string IP = "146.193.226.21";

    // Local Host IP
    // private static readonly string IP = "192.168.1.101";
    private static readonly string PORT = "8080";
    private static string iepResult = "";

    public void LoadDescritpion(string description)
    {
        this._description = description;
        pinging = true;

       StartCoroutine(PingServer());
      
    }



    IEnumerator PingServer()
    {
        WaitForSeconds f = new WaitForSeconds(0.5f);

        UnityEngine.Ping p = new UnityEngine.Ping(IP);

        while (p.isDone == false)
        {
            yield return f;
        }
       
            reachedServer = true;
        

    }


      IEnumerator SendDescription() {

        /*   sendingRequest = true;
           WWWForm form = new WWWForm();
           form.AddField("User-Agent", "Anything");

           UnityWebRequest www = UnityWebRequest.Post(IP, form);
           yield return www.SendWebRequest();

          */
        WaitForSeconds f = new WaitForSeconds(0.5f);

        ProcessDescriptionAsync(_description).GetAwaiter().GetResult();

        while(iepResult == "")
        {
            yield return f; 
           
        }
        Debug.Log("Got the result");
        ComputeStory(iepResult);
       
      
      }
    


    static async Task ProcessDescriptionAsync(string description)
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Anything");

        try
        {

            //Send the Story
            await SendDescriptionAsync(description);


            //Collect the results
            await GetScenarioAsync().ConfigureAwait(false);

        }
        catch (Exception f)
        {
            // Discard PingExceptions and return false;

          Debug.Log(f.Message);


        }





    }


    static async Task GetScenarioAsync()
    {
        Debug.Log("Getting the result");
        var responseBytes = client.GetByteArrayAsync("http://" + IP + ":" + PORT).Result;

        iepResult = Encoding.Default.GetString(responseBytes);


    }

    static async Task<HttpResponseMessage> SendDescriptionAsync(string description)
    {
        // Testing purposes

        Debug.Log("Sent the story");
        var content = new StringContent(description);

        //var response = await client.PostAsync("http://" + IP + ":" + PORT, content).ConfigureAwait(false);

        return Task.Run(() => client.PostAsync("http://" + IP + ":" + PORT, content)).Result;

        /*          

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return;
        }

        else if( response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            MessageBox.Show("No description available");

        }

        //response.EnsureSuccessStatusCode();
        //return response.Headers.Location;*/
    }



    private void ComputeStory(string extrapolations)
    {
        char[] stop = new char[] { '{', '}' };

        var split = extrapolations.Split(stop);

        var DomainKnowledge = split[1];

        var Agents = split[2].Split('\n');


        foreach (var a in Agents)
        {

            if (a.Length < 2)
            {
                continue;
            }

            var parameterSplit = a.Split('%');

            var name = parameterSplit[0];
            var actions = parameterSplit[1];
            var beliefs = parameterSplit[2];
            var needs = parameterSplit[3];


            if (_iat.Characters.Count() == 0)
                _iat.AddNewCharacter((Name)name);

            else if (_iat.Characters.ToList().FindIndex(x => name.Contains(x.CharacterName.ToString())) < 0)
                _iat.AddNewCharacter((Name)name);


            var beliefSplit = beliefs.Split('|');

            // Computing beliefs: loves(banana[Object])

            foreach (var b in beliefSplit)
            {
                if (b.Length < 3)
                    continue;


                var beliefName = b;
                beliefName = beliefName.Replace(" ", "");
                beliefName = beliefName.Replace("]", "");
                beliefName = beliefName.Replace(")", "");

                // Computing beliefs: loves(banana[Object

                var beliefSplitAux = beliefName.Split('(', '[');

                var bNmae = beliefSplitAux[0];
                var bValue = beliefSplitAux[1];

                var rpc = _iat.Characters.First(x => x.CharacterName == (Name)name);

                if (b.Contains('['))
                {
                    var bTarget = beliefSplitAux[2];

                    rpc.UpdateBelief("Is" + "(" + bValue + ")", bTarget);
                }


                rpc.UpdateBelief(bNmae + "(" + bValue + ")", "True");



            }

            var actionSplit = actions.Split('|');

            foreach (var act in actionSplit)
            {
                if (act.Length < 3)
                    continue;

                // go(park[Location])

                // we want a belief and an action: is(Park) = Location and go(park)


                var actName = "";
                var targetName = "";
                var targetValue = "";



                var actionName = act;
                actionName = actionName.Replace(" ", "");
                actionName = actionName.Replace("]", "");
                actionName = actionName.Replace(")", "");

                // go(park[location

                actionSplit = actionName.Split('(', '[');

                actName = actionSplit[0];
                targetName = actionSplit[1];

                if (act.Contains('['))
                {
                    targetValue = actionSplit[2];

                    var rpc = _iat.Characters.First(x => x.CharacterName == (Name)name);

                    rpc.UpdateBelief("Is" + "(" + targetName + ")", targetValue);
                }


             




            }

            var needSplit = needs.Split('|');

            foreach (var ned in needSplit)
            {
                if (ned.Length < 3)
                    continue;

                // go(park[Location])

                // we want a belief and an action: is(Park) = Location and go(park)


                var nedName = "";
                var targetName = "";
                var targetValue = "";



                var needName = ned;
                needName = needName.Replace(" ", "");
                needName = needName.Replace("]", "");
                needName = needName.Replace(")", "");

                // go(park[location

                var needAuxSplit = needName.Split('(', '[');

                nedName = needAuxSplit[0];
                targetName = needAuxSplit[1];

                var rpc = _iat.Characters.First(x => x.CharacterName == (Name)name);

                if (ned.Contains('['))
                {
                    targetValue = actionSplit[2];



                    rpc.UpdateBelief("Is" + "(" + targetName + ")", targetValue);
                }

                rpc.AddOrUpdateGoal(new EmotionalAppraisal.DTOs.GoalDTO()
                {
                    Name = targetName,
                    Likelihood = 0.5f,
                    Significance = 1.0f
                });



            }


        }


    }
}
