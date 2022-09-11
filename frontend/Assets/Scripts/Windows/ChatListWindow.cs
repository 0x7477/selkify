using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class ChatListWindow : Window
{

    public GameObject ChatListEntry;

    public Button createChat;
    public Button findChat;
    public Transform chatlistContent;

    public override string GetID()
    {
        return "CHAT_LIST";
    }

    public void InitChatList(JArray chats)
    {
        Clear(chatlistContent);
        foreach (JObject chat in chats)
        {
            GameObject go = Instantiate(ChatListEntry, chatlistContent);
            go.GetComponentInChildren<TMPro.TMP_Text>().text = (string)chat["NAME"];
            go.GetComponent<Button>().onClick.AddListener(async () => { await WindowManager.Navigate("CHAT", new string[] { (string)chat["ID"] }); });
        }
    }

    public async Task LoadChats()
    {
        await WindowManager.StartLoading();
        InitChatList(JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat")));
        await WindowManager.StopLoading();
    }

    public override async Task Init()
    {
        createChat.onClick.RemoveAllListeners();
        findChat.onClick.RemoveAllListeners();
        createChat.onClick.AddListener(async () => { await WindowManager.Navigate("CREATE_CHAT"); });
        findChat.onClick.AddListener(async () => { await WindowManager.Navigate("FIND_CHAT"); });
        await LoadChats();

    }

    public override async Task Back()
    {
        await WindowManager.Navigate("MENU");
    }
}
