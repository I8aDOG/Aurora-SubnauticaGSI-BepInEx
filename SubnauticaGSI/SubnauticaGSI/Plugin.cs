using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubnauticaGSI
{
    public enum PlayerState
    {
        Menu,
        Loading,
        Playing
    }

    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string pluginGUID = "com.notcasey.SubnauticaGSI";
        private const string pluginName = "SubnauticaGSI";
        private const string pluginVersion = "1.0.0";

        public static PlayerState state;

        private static string currentSceneName;

        private string jsonlast;

        private void Start()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            // Prevent SceneCleaner from deleting the mod.
            gameObject.AddComponent<SceneCleanerPreserve>();
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            currentSceneName = scene.name;
            
            if (currentSceneName.ToLower().Contains("menu"))
            {
                state = PlayerState.Menu;
                SerializeJson();
            }
        }

        private void Update()
        {
            if (currentSceneName == null) return;

            UpdateState();
            SerializeJson();
        }

        private void UpdateState()
        {
            PlayerState newstate = state;

            if (currentSceneName.ToLower().Contains("menu"))
                state = PlayerState.Menu;
            else if (WaitScreen.IsWaiting)
                state = PlayerState.Loading;
            else
                state = PlayerState.Playing;
        }

        private void SerializeJson()
        {
            string json = JsonConvert.SerializeObject(new GSINode(), Formatting.Indented);

            if (json != jsonlast)
            {
                jsonlast = json;
                Send(json);
            }

        }

        public static void Send(string json)
        {
            new Thread(() => //Do not slowdown main thread (result in Game stutter)
            {
                try
                {
                    int AuroraPort = 9088;
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://localhost:" + AuroraPort);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                }
                catch (InvalidCastException e) //ignore Server issues (Aurora closed)
                {
                }
            }).Start();
        }
    }
}
