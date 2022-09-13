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

    public Button back, save, delete;
    public TMPro.TMP_InputField chatname, tags, description;
    public GameObject MemberEntry;
    public Transform content;

    public int chat_id;
    string[] init_args;
    public override string GetID()
    {
        return "CHAT_SETTINGS";
    }

    public void InitSettings(JObject settings)
    {
        chatname.text = (string)settings["NAME"];
        tags.text = (string)settings["TAGS"];
        description.text = (string)settings["DESCRIPTION"];
    }

    public void InitMember(JArray members)
    {
        var options = new List<string>() { "ADMIN", "USER", "KICK" };

        Clear(content, 6, 0);

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

    public void InitDelete(int id)
    {
        delete.onClick.AddListener(async () =>
        {
            await NetworkManager.Get(AppSettings.API_URL + "chat/" + chat_id + "/delete");
            await WindowManager.Navigate("CHAT_LIST");
        });
    }

    public static bool ContainsString(JArray jArray, string s)
    {
        return jArray.Any(x => x.ToString().Equals(s));
    }
    public async Task LoadSettings(int id)
    {
        await WindowManager.StartLoading();
        var permissions = JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/permissions"));

        Debug.Log(permissions);
        if (ContainsString(permissions, "EDIT"))
            InitSettings(JObject.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/info")));

        if (ContainsString(permissions, "READ"))
            InitMember(JArray.Parse(await NetworkManager.Get(AppSettings.API_URL + "chat/" + id + "/users")));

        if (ContainsString(permissions, "DELETE"))
            InitDelete(id);

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
