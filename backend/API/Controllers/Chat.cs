using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace test.Controllers;

using System.Text;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{

    
    [HttpPost("{chat_id}/user/{user_id}/role")]
    public async Task<IActionResult> SetRole(int chat_id, int user_id, [FromForm]string role, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        if(!await HasUserChatPermission(id, chat_id, "EDIT")) return Unauthorized();

        using (var cmd = await DB.getCommand(@"UPDATE CHAT_USER SET ROLE = @role WHERE CHAT_ID = @chat AND USER_ID = @user"))
        {
            cmd.Parameters.AddWithValue("@user", user_id);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@chat", chat_id);

            return Ok(await DB.execSQL(cmd));
        }
    }

    [HttpGet("{chat_id}/join")]
    public async Task<IActionResult> Join(int chat_id, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (var cmd = await DB.getCommand(@"SELECT POLICY FROM CHAT_TYPE_POLICY p INNER JOIN CHAT c ON c.TYPE = p.TYPE WHERE c.ID = @chat_id AND p.POLICY = ""OPEN"";"))
        {

            cmd.Parameters.AddWithValue("@chat_id", chat_id);

            var r = await DB.readSQL(cmd);
            await r.ReadAsync();
            if (r.HasRows)
            {

                await r.CloseAsync();
                return BadRequest("Chat is not public");
            }

            
        }

        using (var cmd = await DB.getCommand(@"INSERT INTO CHAT_USER (USER_ID, CHAT_ID, ROLE) VALUES (@id, @chat_id, ""USER"");"))
        {
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@chat_id", chat_id);

            return Ok(await DB.execSQL(cmd));
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (var cmd = await DB.getCommand(@"(SELECT ID, NAME, TYPE, TAGS FROM SELK_APP.CHAT WHERE TYPE = ""GROUP"" AND CONCAT(TAGS, ' ', NAME) 
        LIKE @query) LIMIT @limit " // AND CHAT_PRIVACY_OPTIONS_NAME = 'CLOSED' 
        // @"UNION SELECT u.ID, u.NAME, u.USER_TYPE_NAME TYPE FROM USER u 
        // INNER JOIN USER_TYPE_PERMISSION p ON p.USER_TYPE_NAME = u.USER_TYPE_NAME WHERE CONCAT(u.USER_TYPE_NAME, ' ', NAME) 
        // LIKE @query AND ID != ? AND p.USER_PERMISSIONS_PERMISSION = 'VISIBLE') LIMIT @limit;"
        ))
        {


            cmd.Parameters.AddWithValue("@query", query ?? "");
            cmd.Parameters.AddWithValue("@limit", 10);
            return Ok(await DB.readJSONSQL(cmd, DB.JSONFormat.Array));
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> Get([FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (var cmd = await DB.getCommand("SELECT * FROM SELK_APP.CHAT_USER cu INNER JOIN CHAT c ON c.ID = cu.CHAT_ID WHERE USER_ID = @id"))
        {
            cmd.Parameters.AddWithValue("@id", id);
            return Ok(await DB.readJSONSQL(cmd, DB.JSONFormat.Array));
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromForm] string name, [FromForm] string type, [FromForm] string description, [FromForm] string tags, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        long insert_id = 0;
        using (var cmd = await DB.getCommand("INSERT INTO CHAT (TYPE, NAME, DESCRIPTION, TAGS) VALUES (@type, @name, @desc, @tags)"))
        {
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@type", type);
            if ((await DB.execSQL(cmd)) != 1) return Problem();

            insert_id = cmd.LastInsertedId;
        }

        using (var cmd = await DB.getCommand("INSERT IGNORE INTO CHAT_USER (USER_ID, CHAT_ID, ROLE) VALUES(@user, @chat, \"ADMIN\")"))
        {
            cmd.Parameters.AddWithValue("@user", id);
            cmd.Parameters.AddWithValue("@chat", insert_id);

            return Ok(await DB.execSQL(cmd));
        }
    }


    [HttpPost("{chat}")]
    public async Task<IActionResult> SendMessage(int chat, [FromForm] string message, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to send message?
        if (!await HasUserChatPermission(id, chat, "WRITE")) return Unauthorized();

        //everything is allright



        MySqlConnector.MySqlDataReader r;
        using (var cmd = await DB.getCommand("SELECT cu.USER_ID, u.PUBLIC_KEY, GROUP_CONCAT(d.ID) \"DEVICES\" FROM CHAT_USER cu INNER JOIN USER u ON u.ID = cu.USER_ID LEFT JOIN DEVICE d ON d.USER_ID = u.ID AND d.USER_ID != @user WHERE CHAT_ID = @chat GROUP BY u.ID;"))
        {
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@user", id);

            r = await DB.readSQL(cmd);
        }

        int message_id = 0;

        List<string> ids = new List<string>();
        while (await r.ReadAsync())
        {
            int user_id = r.GetInt32("USER_ID");
            string key = r.GetString("PUBLIC_KEY");


            if (!r.IsDBNull(2))
            {
                ids.AddRange(r.GetString("DEVICES").Split(","));
            }

            string cipher = Encryption.AsymmetricEncrypt(message, key);

            using (var insertcmd = await DB.getCommand("INSERT INTO CHAT_MESSAGE (AUTHOR, RECEIVER, MESSAGE, CHAT_ID) VALUES(@author, @rec, @msg, @chat)"))
            {
                insertcmd.Parameters.AddWithValue("@author", id);
                insertcmd.Parameters.AddWithValue("@chat", chat);
                insertcmd.Parameters.AddWithValue("@rec", user_id);
                insertcmd.Parameters.AddWithValue("@msg", cipher);

                await DB.execSQL(insertcmd);

                if (user_id == id)
                {
                    //We an to return the id of the inserted message for the user
                    message_id = (int)insertcmd.LastInsertedId;
                }
            }
        }
        await r.CloseAsync();


        if (ids.Count > 0)
        {
            HttpClient clientTest = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");

            string title = "Neue Nachricht";
            string body = "Ist bestimmt ganz ganz wichtig";
            string tokens = "";
            foreach (string s in ids)
            {
                tokens += '"' + s + "\",";
            }
            tokens = tokens.Substring(0, tokens.Length - 1);
            var content = "{\"notification\": {\"title\": \"" + title + "\",\"body\": \"" + body + "\"},\"registration_ids\":[" + tokens + "]}";

            httpRequest.Content = new StringContent(content, Encoding.UTF8, "application/json");
            httpRequest.Headers.TryAddWithoutValidation("Authorization", Settings.ServerKey);

            var response = await clientTest.SendAsync(httpRequest);
        }
        return Ok(message_id);

    }

    [HttpGet("{chat}/users")]
    public async Task<IActionResult> GetUsers(int chat, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to view?
        if (!await HasUserChatPermission(id, chat, "READ")) return Unauthorized();

        //everything is allright

        using (var cmd = await DB.getCommand("SELECT ID, ROLE, USERNAME FROM CHAT_USER c INNER JOIN USER u ON u.ID = c.USER_ID WHERE CHAT_ID = @chat"))
        {
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await DB.readJSONSQL(cmd, DB.JSONFormat.Array));
        }
    }

    [HttpGet("{chat}/info")]
    public async Task<IActionResult> GetInfo(int chat, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to view?
        if (!await HasUserChatPermission(id, chat, "READ")) return Unauthorized();

        //everything is allright

        using (var cmd = await DB.getCommand("SELECT NAME, TAGS, DESCRIPTION, TYPE FROM CHAT WHERE ID = @chat"))
        {
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await DB.readJSONSQL(cmd));
        }
    }

    [HttpPost("{chat}/info")]
    public async Task<IActionResult> SetInfo(int chat, [FromForm] string name, [FromForm] string tags, [FromForm] string description, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to send message?
        if (!await HasUserChatPermission(id, chat, "EDIT")) return Unauthorized();

        //everything is allright

        using (var cmd = await DB.getCommand("UPDATE CHAT SET NAME = @name, TAGS = @tags, DESCRIPTION = @desc WHERE ID = @chat;"))
        {
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await DB.execSQL(cmd));
        }
    }

    [HttpGet("{chat}")]
    public async Task<IActionResult> GetMessages(int chat, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to send message?
        if (!await HasUserChatPermission(id, chat, "READ")) return Unauthorized();

        //everything is allright

        MySqlConnector.MySqlDataReader r;
        using (var cmd = await DB.getCommand("SELECT m.*, u.USERNAME FROM CHAT_MESSAGE m INNER JOIN USER u ON u.ID = m.AUTHOR WHERE CHAT_ID = @chat AND RECEIVER = @id ORDER BY SEND_TIME ASC"))
        {
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@id", id);
            r = await DB.readSQL(cmd);
        }

        string private_key = await AccountController.GetPrivateKey(email, password);


        List<dynamic> messages = new List<dynamic>();
        while (await r.ReadAsync())
        {

            string msg = Encryption.AsymmetricDecrypt(r.GetString("MESSAGE"), private_key);
            int author = r.GetInt32("AUTHOR");
            string time = r.GetDateTime("SEND_TIME").ToString("yyyy-MM-dd HH:mm:ss");
            string username = r.GetString("USERNAME");
            int msg_id = r.GetInt32("ID");
            messages.Add(new { message = msg, author = author, send_at = time, username = username, message_id = msg_id });
        }
        await r.CloseAsync();

        using (var cmd = await DB.getCommand("SELECT NAME, TAGS, DESCRIPTION FROM CHAT WHERE ID = @chat"))
        {
            cmd.Parameters.AddWithValue("@chat", chat);

            var r2 = await DB.readSQL(cmd);
            await r2.ReadAsync();
            var name = r2.GetString("NAME");
            var tags = r2.GetString("TAGS");
            var description = r2.GetString("DESCRIPTION");

            await r.CloseAsync();

            return Ok(new {name = name, tags = tags, description = description, messages = messages });
        }
    }

    [NonAction]
    public async Task<bool> HasUserChatPermission(int user, int chat, string permission)
    {
        using (var cmd = await DB.getCommand("SELECT COUNT(*) FROM SELK_APP.CHAT_USER WHERE USER_ID = @user AND CHAT_ID = @chat AND ROLE IN (SELECT CHAT_ROLE FROM CHAT_ROLE_PERMISSION WHERE CHAT_PERMISSION = @permission);"))
        {
            cmd.Parameters.AddWithValue("@user", user);
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@permission", "WRITE");

            var r = await DB.readSQL(cmd);
            await r.ReadAsync();

            var hasPermission = r.GetInt32(0);
            await r.CloseAsync();
            return hasPermission > 0;
        }
    }



}
