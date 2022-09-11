using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class LoginWindow : Window
{
    public TMPro.TMP_InputField email, password;
    public Button login, register;
    public override string GetID()
    {
        return "LOGIN";
    }

    async Task Login()
    {
        UserInformation.email = email.text;
        UserInformation.password = password.text;

        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.GetWR(AppSettings.API_URL + "account");
        await WindowManager.StopLoading();

        //Request header: raw source
        if (s.result != UnityWebRequest.Result.Success)
        {
            UserInformation.email = "";
            UserInformation.password = "";
            return;
        }

        JObject res = JObject.Parse(s.downloadHandler.text);
        UserInformation.privateKey = (string)res["privateKey"];
        UserInformation.publicKey = (string)res["publicKey"];
        UserInformation.username = (string)res["username"];
        UserInformation.id = (int)res["id"];
        UserInformation.Save();

        await WindowManager.Navigate("MENU");

        await PushNotifications.uploadToken();

    }


    public override async Task Init()
    {
        login.onClick.RemoveAllListeners();
        register.onClick.RemoveAllListeners();

        login.onClick.AddListener(async () => { await Login(); });
        register.onClick.AddListener(async () => { await WindowManager.Navigate("REGISTER"); });

        email.text = UserInformation.email;
        password.text = UserInformation.password;

        await Task.CompletedTask;
    }

    public override async Task Back()
    {
        Application.Quit();
        await Task.CompletedTask;
    }
}
