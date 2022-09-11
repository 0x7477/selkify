using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System;

public class ChatWindow : Window
{

    public GameObject MessageGameobject_Me, MessageGameobject_Them, Message_Info;

    public Button send, back, settings, scrollDown;
    public TMPro.TMP_InputField input;
    public TMPro.TMP_Text chatname;

    public DateTime latestmessage = DateTime.MinValue;
    public Transform chathistoryContent;
    public ScrollRect rect;

    private TMPro.TMP_Text t;
    public int id;

    public override string GetID()
    {
        return "CHAT";
    }

    public void AddMessage(Message message)
    {

        

        //we assume that any new message will be later than any before
        var message_date = DateTime.Parse(message.time);

        if (message_date.Date != latestmessage.Date)
        {
            //lets create Date Label
            GameObject label = Instantiate(Message_Info, chathistoryContent);

            string date = "";
            if (DateTime.Today.Subtract(message_date).TotalDays <= 7) 
                date = message_date.DayOfWeek.ToString();
            else if (message_date.Date.Year != latestmessage.Date.Year) 
                date = message_date.ToString("dd.MM.yyyy");
            else
                date = message_date.ToString("dd.MM");

            label.GetComponentInChildren<TMPro.TMP_Text>().text = date;

        }
        latestmessage = message_date;

        GameObject go = Instantiate((message.author == UserInformation.id) ? MessageGameobject_Me : MessageGameobject_Them, chathistoryContent);
        go.name = message.id.ToString();
        t = go.GetComponentInChildren<TMPro.TMP_Text>();

        go.transform.Find("TEXT/PANEL/TIME").GetComponent<TMPro.TMP_Text>().text = message.time.Substring(11, 5);
        go.transform.Find("TEXT/PANEL/USER").GetComponent<TMPro.TMP_Text>().text = message.username;
        t.text = message.message;
    }
    public void InitChat(JObject messages)
    {
        Clear(chathistoryContent);
        chatname.text = (string)messages["name"];

        foreach (JObject message in messages["messages"])
        {
            Message m = new Message();
            m.author = (int)message["author"];
            m.username = (string)message["username"];
            m.id = (int)message["message_id"];
            m.message = (string)message["message"];
            m.time = (string)message["send_at"];

            AddMessage(m);
        }
    }

    IEnumerator ForceScrollDown()
    {
        // Wait for end of frame AND force update all canvases before setting to bottom.
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        rect.content.localPosition = Vector3.zero;
        Canvas.ForceUpdateCanvases();
    }

    public async Task SendMessage(int chat_id)
    {
        Dictionary<string, string> form = new Dictionary<string, string>()
        {
            {"message", input.text}
        };

        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.PostWR(AppSettings.API_URL + "chat/" + chat_id, form);

        await WindowManager.StopLoading();

        if (s.result != UnityWebRequest.Result.Success)
        {
            await WindowManager.Error(s.downloadHandler.text);
            return;
        }

        Message m = new Message();
        m.username = UserInformation.username;
        m.author = UserInformation.id;
        m.id = int.Parse(s.downloadHandler.text);
        m.message = (string)form["message"];
        m.time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        AddMessage(m);

        //Scroll to bottom
        StartCoroutine(ForceScrollDown());



    }

    public async Task LoadChat(int id)
    {
        await WindowManager.StartLoading();

        JObject o = JObject.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id));
        InitChat(o);
        await WindowManager.StopLoading();
    }


    public override async Task Init(string[] args)
    {
        int chat_id = int.Parse(args[0]);

        scrollDown.onClick.RemoveAllListeners();
        send.onClick.RemoveAllListeners();
        back.onClick.RemoveAllListeners();
        settings.onClick.RemoveAllListeners();

        scrollDown.onClick.AddListener(() => { StartCoroutine(ForceScrollDown()); });
        send.onClick.AddListener(async () => { await SendMessage(chat_id); });
        back.onClick.AddListener(async () => { await Back(); });
        settings.onClick.AddListener(async () => { await WindowManager.Navigate("CHAT_SETTINGS", args); });

        await LoadChat(chat_id);
    }

    public override async Task Back()
    {
        await WindowManager.Navigate("CHAT_LIST");
    }

    public class Message
    {
        public int id;
        public int author;
        public string username;
        public string message;
        public string time;
    }
}
