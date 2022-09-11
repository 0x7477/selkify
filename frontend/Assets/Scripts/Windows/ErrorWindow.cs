using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ErrorWindow : Window
{
    public TMPro.TMP_Text error;
    public Button close;

    public override string GetID()
    {
        return "ERROR";
    }
    public override async Task Init(string[] args)
    {
        error.text = args[0];
        await Task.CompletedTask;   

        close.onClick.RemoveAllListeners();

        close.onClick.AddListener(async () => {await Hide();});
    }

    public override async Task Back()
    {
        await Close();
    }
}
