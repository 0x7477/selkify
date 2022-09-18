using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace test.Controllers;

using System.Data;
using System.Text;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{


    [HttpPost("{chat_id}/user/{user_id}/role")]
    public async Task<IActionResult> SetRole(int chat_id, int user_id, [FromForm] string role, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        if (!await HasUserChatPermission(id, chat_id, "EDIT")) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"UPDATE CHAT_USER SET ROLE = @role WHERE CHAT_ID = @chat AND USER_ID = @user");
            cmd.Parameters.AddWithValue("@user", user_id);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@chat", chat_id);

            return Ok(await db.exec());
        }
    }


    [HttpGet("{chat_id}/leave")]
    public async Task<IActionResult> Leave(int chat_id, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"SELECT USER_ID FROM SELKIFY.CHAT_USER WHERE USER_ID != @user_id AND ROLE = ""ADMIN"";");
            cmd.Parameters.AddWithValue("@user_id", id);

            var table = await db.read();

            if(table.Rows.Count == 0) return Problem("Group would have no Admin");
        }

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"DELETE FROM CHAT_USER WHERE CHAT_ID = @chat_id AND USER_ID = @user_id;");
            cmd.Parameters.AddWithValue("@chat_id", chat_id);
            cmd.Parameters.AddWithValue("@user_id", id);
            return Ok(await db.exec());
        }
    }

    [HttpGet("{chat_id}/delete")]
    public async Task<IActionResult> Delete(int chat_id, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        if (!await HasUserChatPermission(id, chat_id, "DELETE")) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"DELETE FROM CHAT WHERE ID = @chat_id;");
            cmd.Parameters.AddWithValue("@chat_id", chat_id);
            return Ok(await db.exec());
        }
    }

    [HttpGet("{chat_id}/join")]
    public async Task<IActionResult> Join(int chat_id, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"SELECT POLICY FROM CHAT_TYPE_POLICY p INNER JOIN CHAT c ON c.TYPE = p.TYPE WHERE c.ID = @chat_id AND p.POLICY = ""OPEN"";");
            cmd.Parameters.AddWithValue("@chat_id", chat_id);

            var table = await db.read();
            if (table.Rows.Count == 0)
                return BadRequest("Chat is not public");
        }

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"INSERT INTO CHAT_USER (USER_ID, CHAT_ID, ROLE) VALUES (@id, @chat_id, ""USER"");");
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@chat_id", chat_id);

            return Ok(await db.exec());
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD(@"(SELECT ID, NAME, TYPE, TAGS FROM CHAT WHERE TYPE = ""GROUP"" AND CONCAT(COALESCE(TAGS,''), ' ', NAME) 
        LIKE @query) LIMIT @limit " // AND CHAT_PRIVACY_OPTIONS_NAME = 'CLOSED' 
                                                                    // @"UNION SELECT u.ID, u.NAME, u.USER_TYPE_NAME TYPE FROM USER u 
                                                                    // INNER JOIN USER_TYPE_PERMISSION p ON p.USER_TYPE_NAME = u.USER_TYPE_NAME WHERE CONCAT(u.USER_TYPE_NAME, ' ', NAME) 
                                                                    // LIKE @query AND ID != ? AND p.USER_PERMISSIONS_PERMISSION = 'VISIBLE') LIMIT @limit;"
        );

            cmd.Parameters.AddWithValue("@query", query ?? "");
            cmd.Parameters.AddWithValue("@limit", 10);
            return Ok(await db.readJSON());
        }
    }



    [HttpGet("types")]
    public async Task<IActionResult> GetTypes([FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT * FROM CHAT_TYPE cu INNER JOIN CHAT c ON c.ID = cu.CHAT_ID WHERE USER_ID = @id");
            cmd.Parameters.AddWithValue("@id", id);
            return Ok(await db.readJSON());
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> Get([FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT * FROM CHAT_USER cu INNER JOIN CHAT c ON c.ID = cu.CHAT_ID WHERE USER_ID = @id");
            cmd.Parameters.AddWithValue("@id", id);
            return Ok(await db.readJSON());
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromForm] string name, [FromForm] string type, [FromForm] string? description, [FromForm] string? tags, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        long insert_id = 0;

        using (DB db = new DB())
        {
            var cmd = db.getCMD("INSERT INTO CHAT (TYPE, NAME, DESCRIPTION, TAGS) VALUES (@type, @name, @desc, @tags)");
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@type", type);

            if ((await db.exec()) != 1) return Problem();

            insert_id = cmd.LastInsertedId;
        }

        using (DB db = new DB())
        {
            var cmd = db.getCMD("INSERT IGNORE INTO CHAT_USER (USER_ID, CHAT_ID, ROLE) VALUES(@user, @chat, \"ADMIN\")");
            cmd.Parameters.AddWithValue("@user", id);
            cmd.Parameters.AddWithValue("@chat", insert_id);

            await db.exec();

            return Ok(insert_id);
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

        List<string> ids = new List<string>();
        int message_id = 0;
        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT cu.USER_ID, u.PUBLIC_KEY, GROUP_CONCAT(d.ID) \"DEVICES\" FROM CHAT_USER cu INNER JOIN USER u ON u.ID = cu.USER_ID LEFT JOIN DEVICE d ON d.USER_ID = u.ID AND d.USER_ID != @user WHERE CHAT_ID = @chat GROUP BY u.ID;");
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@user", id);

            var table = await db.read();


            foreach (DataRow row in table.Rows)
            {
                int user_id = Convert.ToInt32(row["USER_ID"]);
                string key = Convert.ToString(row["PUBLIC_KEY"]) ?? "";

                string devices = Convert.ToString(row["DEVICES"]) ?? "";
                if (devices != "")
                {
                    ids.AddRange(devices.Split(","));
                }

                string cipher = Encryption.AsymmetricEncrypt(message, key);

                var insertcmd = db.getCMD("INSERT INTO CHAT_MESSAGE (AUTHOR, RECEIVER, MESSAGE, CHAT_ID) VALUES(@author, @rec, @msg, @chat)");

                insertcmd.Parameters.AddWithValue("@author", id);
                insertcmd.Parameters.AddWithValue("@chat", chat);
                insertcmd.Parameters.AddWithValue("@rec", user_id);
                insertcmd.Parameters.AddWithValue("@msg", cipher);

                await db.exec();

                
                if (user_id == id)
                    //We an to return the id of the inserted message for the user
                    message_id = (int)insertcmd.LastInsertedId;
                
            }
        }

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

            await clientTest.SendAsync(httpRequest);
        }
        return Ok(message_id);

    }

    [HttpPost("{chat}/type")]
    public async Task<IActionResult> SetType(int chat, [FromForm] string type, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to send message?
        if (!await HasUserChatPermission(id, chat, "EDIT")) return Unauthorized();

        //everything is allright

        using (DB db = new DB())
        {
            var cmd = db.getCMD("UPDATE CHAT SET TYPE = @type WHERE ID = @chat;");
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await db.exec());
        }
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

        using (DB db = new DB())
        {
            var cmd =  db.getCMD("SELECT ID, ROLE, USERNAME FROM CHAT_USER c INNER JOIN USER u ON u.ID = c.USER_ID WHERE CHAT_ID = @chat");
            cmd.Parameters.AddWithValue("@chat", chat);
            return Ok(await db.readJSON());
        }
    }

    [HttpGet("{chat}/permissions")]
    public async Task<IActionResult> GetPermissions(int chat, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to view?

        //everything is allright

        using (DB db = new DB())
        {

        var cmd =  db.getCMD("SELECT PERMISSION FROM CHAT_ROLE_PERMISSION p INNER JOIN CHAT_USER u ON u.ROLE = p.ROLE WHERE u.USER_ID = @user_id AND u.CHAT_ID = @chat_id;");
            cmd.Parameters.AddWithValue("@chat_id", chat);
            cmd.Parameters.AddWithValue("@user_id", id);

            return Ok(await db.readJSON());
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

        using (DB db = new DB())
        {

            var cmd = db.getCMD("SELECT NAME, TAGS, DESCRIPTION, TYPE FROM CHAT WHERE ID = @chat");
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await db.readJSON());
        }
    }

    [HttpPost("{chat}/info")]
    public async Task<IActionResult> SetInfo(int chat, [FromForm] string name, [FromForm] string? tags, [FromForm] string? description, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await AccountController.GetID(email, password);
        if (id == 0) return Unauthorized();

        //we authentificated our user
        //is our User authentificated to send message?
        if (!await HasUserChatPermission(id, chat, "EDIT")) return Unauthorized();

        //everything is allright

        using (DB db = new DB())
        {
            var cmd = db.getCMD("UPDATE CHAT SET NAME = @name, TAGS = @tags, DESCRIPTION = @desc WHERE ID = @chat;");
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@chat", chat);

            return Ok(await db.exec());
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

        DataTable messageTable;
        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT m.*, u.USERNAME FROM CHAT_MESSAGE m INNER JOIN USER u ON u.ID = m.AUTHOR WHERE CHAT_ID = @chat AND RECEIVER = @id ORDER BY SEND_TIME ASC");
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@id", id);
            messageTable = await db.read();
        }

        string private_key = await AccountController.GetPrivateKey(email, password);


        List<dynamic> messages = new List<dynamic>();
        foreach(DataRow row in messageTable.Rows)
        {

            string message = Encryption.AsymmetricDecrypt( Convert.ToString(row["MESSAGE"]) ?? "", private_key);
            int author = Convert.ToInt32(row["AUTHOR"]);
            string send_at = Convert.ToDateTime(row["SEND_TIME"]).ToString("yyyy-MM-dd HH:mm:ss");
            string username = Convert.ToString(row["USERNAME"]) ?? "";
            int message_id = Convert.ToInt32(row["ID"]);
            messages.Add(new { message, author, send_at, username, message_id });
        }

        using (DB db = new DB())
        {

        var cmd =  db.getCMD("SELECT NAME, TAGS, DESCRIPTION FROM CHAT WHERE ID = @chat");
            cmd.Parameters.AddWithValue("@chat", chat);

        var chatInfo =  await db.read();

            var name = Convert.ToString(chatInfo.Rows[0]["NAME"]);
            var tags = Convert.ToString(chatInfo.Rows[0]["TAGS"]);
            var description = Convert.ToString(chatInfo.Rows[0]["DESCRIPTION"]);


            return Ok(new { name, tags, description, messages });
        }
    }

    [NonAction]
    public async Task<bool> HasUserChatPermission(int user, int chat, string permission)
    {
        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT COUNT(*) C FROM CHAT_USER WHERE USER_ID = @user AND CHAT_ID = @chat AND ROLE IN (SELECT ROLE FROM CHAT_ROLE_PERMISSION WHERE PERMISSION = @permission);");
            cmd.Parameters.AddWithValue("@user", user);
            cmd.Parameters.AddWithValue("@chat", chat);
            cmd.Parameters.AddWithValue("@permission", "WRITE");

            var table = await db.read();
            return Convert.ToInt32(table.Rows[0]["C"]) > 0;
        }
    }
}
