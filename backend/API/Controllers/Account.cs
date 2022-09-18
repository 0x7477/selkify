using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace test.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    public static async Task<int> GetID(string email, string password)
    {

        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT ID, PASSWORD, SALT FROM USER WHERE EMAIL = @email;");
            cmd.Parameters.AddWithValue("@email", email);

            var table = await db.read();

            if(table.Rows.Count == 0) return 0;

            var id = Convert.ToInt32(table.Rows[0]["ID"]);
            var pass = Convert.ToString(table.Rows[0]["PASSWORD"]) ?? "";
            var salt = Convert.ToString(table.Rows[0]["SALT"]) ?? "";
            

            if (Encryption.HashPassword(password, salt) == pass) return id;
            return 0;
        }
    }

    [HttpPost("device")]
    public async Task<IActionResult> RegisterDevice([FromForm] string device, [FromHeader] string email, [FromHeader] string password)
    {
        int id = await GetID(email, password);

        using (DB db = new DB())
        {
            var cmd = db.getCMD("INSERT IGNORE INTO DEVICE (ID, USER_ID) VALUES(@device, @id)");
            cmd.Parameters.AddWithValue("@device", device);
            cmd.Parameters.AddWithValue("@id", id);

            await db.exec();

            return Ok("registered device");
        }
    }

    [HttpGet("")]
    public async Task<IActionResult> Login([FromHeader] string email, [FromHeader] string password)
    {
        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT ID, PASSWORD, SALT, PRIVATE_KEY, PUBLIC_KEY, USERNAME FROM USER WHERE EMAIL = @email;");
            cmd.Parameters.AddWithValue("@email", email);

            var table = await db.read();

            if(table.Rows.Count == 0) return Unauthorized();

            var pass = Convert.ToString(table.Rows[0]["PASSWORD"]) ?? "";
            var salt = Convert.ToString(table.Rows[0]["SALT"]) ?? "";

            var id = Convert.ToInt32(table.Rows[0]["ID"]);

            var privateKey = Convert.ToString(table.Rows[0]["PRIVATE_KEY"]);
            var publicKey = Convert.ToString(table.Rows[0]["PUBLIC_KEY"]);
            var username = Convert.ToString(table.Rows[0]["USERNAME"]);


            if (Encryption.HashPassword(password, salt) != pass) return Unauthorized();

            return Ok(new { privateKey, publicKey, id, username });
        }
    }


    [HttpPost("")]
    public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password, [FromForm] string username)
    {
        var rsa = Encryption.generateRSA();
        string salt = Encryption.generateSalt();
        string hashedPassword = Encryption.HashPassword(password, salt);
        string encryptedPrivateKey = Encryption.SymmetricEncrypt(password, rsa.private_key);

        using (DB db = new DB())
        {
            var cmd = db.getCMD("INSERT INTO USER (TYPE, EMAIL, USERNAME, PASSWORD, SALT, PUBLIC_KEY, PRIVATE_KEY) VALUES(\"USER\", @email, @username, @password, @salt, @public_key, @private_key)");
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", hashedPassword);
            cmd.Parameters.AddWithValue("@salt", salt);
            cmd.Parameters.AddWithValue("@public_key", rsa.public_key);
            cmd.Parameters.AddWithValue("@private_key", encryptedPrivateKey);

            await db.exec();
        }


        int id = await GetID(email, password);

        return Ok(new { privateKey = rsa.private_key, publicKey = rsa.public_key, id = id });
    }

    [NonAction]
    public static async Task<string> GetPrivateKey(string email, string password)
    {
        using (DB db = new DB())
        {
            var cmd = db.getCMD("SELECT PRIVATE_KEY FROM USER WHERE EMAIL = @email;");
            cmd.Parameters.AddWithValue("@email", email);

            var table = await db.read();
            if(table.Rows.Count == 0) return "";
            var private_key = Convert.ToString(table.Rows[0]["PRIVATE_KEY"]) ?? "";

            return Encryption.SymmetricDecrypt(password, private_key);
        }
    }
    
}
