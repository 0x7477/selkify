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
        using(DB db = new DB())
        {
            return db.getConnection().State.ToString();
        }
    }

    [HttpGet("calc")]
    public async Task<IActionResult> calc(int? a, int? b)
    {
        using(DB db = new DB())
        {
            
            var cmd = db.getCMD("SELECT @a + @b sum");
            cmd.Parameters.AddWithValue("@a", a ?? 0);
            cmd.Parameters.AddWithValue("@b", b ?? 0);
            
            return Ok(await db.readJSON());
        }
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> calendar()
    {
        using(DB db = new DB())
        {
            db.getCMD("SELECT * FROM CALENDAR;");            
            return Ok(await db.readJSON());
        }

    }
}
