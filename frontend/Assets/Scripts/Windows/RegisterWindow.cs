using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class RegisterWindow : Window
{
    public TMPro.TMP_InputField username, password, email;
    public Button register, login;
    public override string GetID()
    {
        return "REGISTER";
    }

    async Task Register()
    {
        Dictionary<string, string> form = new Dictionary<string, string>()
        {
            {"email", email.text},
            {"username", username.text},
            {"password", password.text}
        };

        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.PostWR(AppSettings.API_URL + "account", form);

        await WindowManager.StopLoading();

        if (s.result != UnityWebRequest.Result.Success)
            return;

        UserInformation.email = form["email"];
        UserInformation.password = form["password"];
        UserInformation.username = form["username"];

        JObject res = JObject.Parse(s.downloadHandler.text);
        UserInformation.privateKey = (string)res["privateKey"];
        UserInformation.publicKey = (string)res["publicKey"];
        UserInformation.id = (int)res["id"];

        UserInformation.Save();

        await WindowManager.Navigate("MENU");

    }

    public override async Task Init()
    {
        register.onClick.RemoveAllListeners();
        login.onClick.RemoveAllListeners();

        register.onClick.AddListener(async () => { await WindowManager.Navigate("LOGIN"); });
        login.onClick.AddListener(async () => { await Register(); });

        await Task.CompletedTask;
    }

    public override async Task Back()
    {
        Application.Quit();
        await Task.CompletedTask;
    }
}
