using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

public static class NetworkManager
{

    public static async Task<UnityWebRequest> GetWR(string url)
    {
        try
        {
            var www = UnityWebRequest.Get(url);

            if(UserInformation.email != null)
                www.SetRequestHeader("email", UserInformation.email);
            if(UserInformation.password != null)
                www.SetRequestHeader("password", UserInformation.password);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{url} Failed: {www.error}");
                Debug.LogError(www.downloadHandler.text);
                await WindowManager.Error(www.error);
            }
            return www;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Webrequest failed: {ex.Message}");
            return default;
        }
    }

    public static async Task<UnityWebRequest> PostWR(string url, Dictionary<string, string> form)
    {
        try
        {
            var www = UnityWebRequest.Post(url, form);

            if(UserInformation.email != null)
            www.SetRequestHeader("email", UserInformation.email);
            if(UserInformation.password != null)
            www.SetRequestHeader("password", UserInformation.password);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{url} Failed: {www.error}");
                await WindowManager.Error(www.error);
            }
            return www;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Webrequest failed: {ex.Message}");
            return default;
        }
    }

    public static async Task<string> Get(string url)
    {
        return (await GetWR(url)).downloadHandler.text;
    }

    public static async Task<string> Post(string url,Dictionary<string, string> form)
    {
        return (await PostWR(url, form)).downloadHandler.text;
    }
}
