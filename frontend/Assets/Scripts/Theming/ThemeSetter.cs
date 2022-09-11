using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeSetter : MonoBehaviour
{
    public Theme.Colors color;
    // Start is called before the first frame update
    void Awake()
    {
        
        if(GetComponent<Image>())
            GetComponent<Image>().color = Theme.themes[color.ToString()];
        
        if(GetComponent<TMPro.TextMeshProUGUI>())
            GetComponent<TMPro.TextMeshProUGUI>().color = Theme.themes[color.ToString()];

    }
}

public static class Theme
{

    public enum Colors 
    {
        main, background, text, accent, error
    }
    public static Dictionary<string,Color> themes = new Dictionary<string, Color>()
    {
        {"main", hex("#0f2d24") },
        {"background", hex("#233153") },
        {"text", hex("#f5f5f5") },
        {"accent", hex("#d84e31") },
        {"error", hex("#ff0000") },
    };

    private static Color hex(string color)
    {
         Color c = new Color();
         ColorUtility.TryParseHtmlString(color, out c);
         return c;
    }

}
