using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class CreateChatWindow : Window
{
    public Button createChat;
    public TMPro.TMP_InputField name_field, description_field, tags_field;

    public override string GetID()
    {
        return "CREATE_CHAT";
    }

    async Task CreateChat()
    {
        Dictionary<string, string> form = new Dictionary<string, string>()
        {
            {"name", name_field.text},
            {"description", description_field.text},
            {"tags", tags_field.text},
            {"type", "GROUP"}
        };

        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.PostWR(AppSettings.API_URL + "chat", form);

        await WindowManager.StopLoading();


        if (s.result != UnityWebRequest.Result.Success)
            return;

        Debug.Log(s.downloadHandler.text);
        string[] strings = { s.downloadHandler.text };
        await WindowManager.Navigate("CHAT", strings);

    }

    public override Task Init()
    {

        createChat.onClick.RemoveAllListeners();
        createChat.onClick.AddListener(async () => { await CreateChat(); });

        return Task.CompletedTask;
    }

    public override async Task Back()
    {
        await WindowManager.Navigate("CHAT_LIST");
    }
}
