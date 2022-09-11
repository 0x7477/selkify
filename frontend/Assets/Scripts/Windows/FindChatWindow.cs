using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class FindChatWindow : Window
{
    public GameObject Chatlistbutton;
    public TMPro.TMP_InputField input;

    public Transform chatlistcontent;


    public override async Task Init()
    {
        input.onValueChanged.RemoveAllListeners();

        input.onValueChanged.AddListener(async (string s) =>
        {
            JArray chats = JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/search?query=%" + s + "%"));

            Clear(chatlistcontent);

            foreach (JObject chat in chats)
            {
                GameObject go = Instantiate(Chatlistbutton, chatlistcontent);
                int id = (int)chat["ID"];
                go.name = id.ToString();
                go.transform.Find("NAME").GetComponent<TMPro.TMP_Text>().text = (string)chat["NAME"];
                go.transform.Find("TAGS").GetComponent<TMPro.TMP_Text>().text = (string)chat["TAGS"];


                go.GetComponent<Button>().onClick.AddListener(async () => { await JoinChat(id); });
            }
        });

        await Task.CompletedTask;
    }

    async Task JoinChat(int id)
    {
        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.GetWR(AppSettings.API_URL + "chat/" + id + "/join");
        await WindowManager.StopLoading();

        if (s.result != UnityWebRequest.Result.Success)
            return;

        await WindowManager.Navigate("CHAT", new string[] { id.ToString() });
    }

    public override async Task Back()
    {
        await WindowManager.Navigate("CHAT_LIST");
    }

    public override string GetID()
    {
        return "FIND_CHAT";
    }
}
