using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Linq;

public class ChatSettingsWindow : Window
{

    public Button back, save, delete, leave;
    public TMPro.TMP_InputField chatname, tags, description;
    public GameObject MemberEntry;

    public TMPro.TMP_Dropdown type;
    public Transform content;

    public int chat_id;
    string[] init_args;
    public override string GetID()
    {
        return "CHAT_SETTINGS";
    }

    public void InitSettings(JArray settings)
    {
        chatname.text = (string)settings[0]["NAME"];
        tags.text = (string)settings[0]["TAGS"];
        description.text = (string)settings[0]["DESCRIPTION"];
    }

    public void InitMember(JArray members)
    {
        var options = new List<string>() { "ADMIN", "USER", "KICK" };

        Clear(content, 7, 0);

        foreach (JObject member in members)
        {
            if (options.IndexOf((string)member["ROLE"]) == -1) continue;
            var go = Instantiate(MemberEntry, content);
            go.name = (string)member["ID"];
            go.transform.Find("NAME").GetComponent<TMPro.TMP_Text>().text = (string)member["USERNAME"];
            var dropdown = go.transform.Find("ROLE").GetComponent<TMPro.TMP_Dropdown>(); //.text = ;

            dropdown.AddOptions(options);

            dropdown.value = options.IndexOf((string)member["ROLE"]);
            dropdown.onValueChanged.AddListener((i) => { UserAction((int)member["ID"], options[i], go); });
        }
    }

    async void UserAction(int id, string action, GameObject go)
    {
        await WindowManager.StartLoading();

        Dictionary<string, string> form = new Dictionary<string, string>() { { "role", action } };
        if (action == "KICK")
        {
            Destroy(go);
            form = new Dictionary<string, string>() { { "role", "BANNED" } };
        }
        await NetworkManager.PostWR(AppSettings.API_URL + "chat/" + chat_id + "/user/" + id + "/role", form);

        await WindowManager.StopLoading();

    }

    public void InitType(JArray settings)
    {
        string type_string = (string)settings[0]["TYPE"];

        //select type
        int index = -1;
        foreach (var i in type.options)
        {
            index++;
            if (i.text != type_string) continue;
            type.value = index;
        }

        type.onValueChanged.AddListener(async (int val) =>
        {
            await WindowManager.StartLoading();

            Dictionary<string, string> form = new Dictionary<string, string>() { { "type", type.options[val].text } };
            var wr = await NetworkManager.PostWR(AppSettings.API_URL + "chat/" + chat_id + "/type", form);

            if (wr.result != UnityWebRequest.Result.Success) return;


            await WindowManager.StopLoading();
            await WindowManager.Navigate("CHAT_LIST");


        });

    }

    public void InitLeave()
    {
        leave.onClick.RemoveAllListeners();
        leave.onClick.AddListener(async () =>
        {
            var wr = await NetworkManager.GetWR(AppSettings.API_URL + "chat/" + chat_id + "/leave");

            if (wr.result != UnityWebRequest.Result.Success) return;

            await WindowManager.Navigate("CHAT_LIST");
        });
    }

    public void InitDelete()
    {
        delete.onClick.RemoveAllListeners();
        delete.onClick.AddListener(async () =>
        {
            var wr = await NetworkManager.GetWR(AppSettings.API_URL + "chat/" + chat_id + "/delete");

            if (wr.result != UnityWebRequest.Result.Success) return;

            await WindowManager.Navigate("CHAT_LIST");
        });
    }

    public static bool ContainsString(JArray jArray, string s)
    {
        foreach (JObject o in jArray)
        {
            if ((string)o["PERMISSION"] == s) return true;
        }
        return false;
    }
    public async Task LoadSettings(int id)
    {
        await WindowManager.StartLoading();
        var permissions = JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/permissions"));

        if (ContainsString(permissions, "EDIT"))
        {
            var info = JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/info"));
            InitSettings(info);
            InitType(info);
        }
        if (ContainsString(permissions, "READ"))
            InitMember(JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/users")));

        if (ContainsString(permissions, "DELETE"))
            InitDelete();

        InitLeave();

        await WindowManager.StopLoading();
    }

    public async Task SaveSettings()
    {
        Dictionary<string, string> form = new Dictionary<string, string>()
        {
            {"name", chatname.text},
            {"tags", tags.text},
            {"description", description.text}
        };

        await WindowManager.StartLoading();
        UnityWebRequest s = await NetworkManager.PostWR(AppSettings.API_URL + "chat/" + chat_id + "/info", form);

        await WindowManager.StopLoading();

        if (s.result != UnityWebRequest.Result.Success)
            return;

        await Back();
    }



    public override async Task Init(string[] args)
    {
        init_args = args;
        chat_id = int.Parse(args[0]);


        save.onClick.RemoveAllListeners();
        back.onClick.RemoveAllListeners();
        save.onClick.AddListener(async () => { await SaveSettings(); });
        back.onClick.AddListener(async () => { await WindowManager.Navigate("CHAT", args); });

        await LoadSettings(chat_id);
    }

    public override async Task Back()
    {
        await WindowManager.Navigate("CHAT", init_args);
    }
}
