using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace test.Controllers;

[ApiController]
[Route("")]
public class MainController : ControllerBase
{

    [HttpGet]
    public string Get()
    {
        return "HI";
    }

    [HttpGet("hallo")]
    public string GetHallo()
    {

        return "hallo";
    }

    [HttpGet("hallo/{name}")]
    public string GetHallo(string name)
    {
        return name;
    }

    [HttpGet("connect")]
    public string connect()
    {
        return "";
        /*
        using(DB db = new DB())
        {
            return db.Connection.State.ToString();
        }
        */
    }

    [HttpGet("calc")]
    public async Task<string> calc(int? a, int? b)
    {
        var cmd = await DB.getCommand("SELECT @a + @b sum");
        cmd.Parameters.AddWithValue("@a", a ?? 0);
        cmd.Parameters.AddWithValue("@b", b ?? 0);
        
        return await DB.readJSONSQL(cmd);
    }

    [HttpGet("calendar")]
    public async Task<string> calendar()
    {
        return await DB.readJSONSQL("SELECT * FROM CALENDAR;");
    }
}
