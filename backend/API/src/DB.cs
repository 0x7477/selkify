using System;
using MySqlConnector;
using Newtonsoft.Json;
using System.Data;
public class DB : IDisposable
{
    private MySqlConnection conn;
    private MySqlCommand? cmd;
    public DB()
    {
        conn = new MySqlConnection(Settings.dbconn);
        conn.Open();
    }

    public void Dispose()
    {
        conn.Close();
    }

    public MySqlConnection getConnection()
    {
        return conn;
    }

    public MySqlCommand getCMD(string sql)
    {
        cmd = new MySqlCommand(sql, conn);
        return cmd;
    }

    public async Task<int> exec()
    {
        if(cmd == null) return 0;
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DataTable> read()
    {
        DataTable dt = new DataTable();
        if(cmd == null) return dt;

        dt.Load(await cmd.ExecuteReaderAsync());
        return dt;
    }

    public async Task<string> readJSON()
    {
        return JsonConvert.SerializeObject(await read());
    }

    /*

    public static async Task<MySqlConnector.MySqlDataReader> readSQL(string sql)
    {
        return await readSQL(await getCommand(sql));
    }

    public static async Task<MySqlConnector.MySqlDataReader> readSQL(MySqlCommand cmd)
    {
        return await cmd.ExecuteReaderAsync();
    }

    public static async Task<int> execSQL(string sql)
    {
        return await execSQL(await getCommand(sql));
    }
    public static async Task<int> execSQL(MySqlCommand cmd)
    {
        return await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<string> readJSONSQL(string sql)
    {
        return await getJSON(await readSQL(sql));
    }

    public static async Task<string> readJSONSQL(MySqlCommand cmd)
    {
        return await getJSON(await readSQL(cmd));
    }

    public static async Task<string> readJSONSQL(MySqlCommand cmd, JSONFormat format)
    {
        return await getJSON(await readSQL(cmd), format);
    }

    public enum JSONFormat { Auto, Array };

    private static async Task<string> getJSON(MySqlConnector.MySqlDataReader r, JSONFormat format = JSONFormat.Auto)
    {
        //return JsonConvert.SerializeObject(r.GetSchemaTable(), Formatting.Indented);
        var cols = await r.GetColumnSchemaAsync();
        List<string[]> rows = new List<string[]>();

        string res = "";

        while (await r.ReadAsync())
        {
            string[] row = new string[cols.Count];

            for (int i = 0; i < cols.Count; i++)
                row[i] = r[i].ToString() ?? "";

            rows.Add(row);
        }

        if (format == JSONFormat.Auto)
        {
            if (rows.Count == 0) return "";
            if (rows.Count == 1)
            {
                if (cols.Count == 1) return printValue(rows[0][0], cols[0].DataTypeName);

                res = "{";

                for (int i = 0; i < cols.Count; i++)
                    res += "\"" + cols[i].ColumnName + "\":" + printValue(rows[0][i], cols[i].DataTypeName) + ",";

                return res.Substring(0, res.Length - 1) + "}";
            }
            else
            {
                res = "[ ";

                for (int j = 0; j < rows.Count; j++)
                {
                    if (cols.Count == 1) { res += printValue(rows[j][0], cols[0].DataTypeName) + ","; continue; }
                    res += "{";

                    for (int i = 0; i < cols.Count; i++)
                        res += "\"" + cols[i].ColumnName + "\":" + printValue(rows[j][i], cols[i].DataTypeName) + ",";

                    res = res.Substring(0, res.Length - 1) + "},";
                }

                res = res.Substring(0, res.Length - 1) + "]";
            }
        }
        else
        {
            res = "[ ";

            for (int j = 0; j < rows.Count; j++)
            {
                if (cols.Count == 1) { res += printValue(rows[j][0], cols[0].DataTypeName) + ","; continue; }
                res += "{";

                for (int i = 0; i < cols.Count; i++)
                    res += "\"" + cols[i].ColumnName + "\":" + printValue(rows[j][i], cols[i].DataTypeName) + ",";

                res = res.Substring(0, res.Length - 1) + "},";
            }

            res = res.Substring(0, res.Length - 1) + "]";
        }

        await r.CloseAsync();
        return res;

    }
    private static string printValue(string val, string? t)
    {
        if (t == "VARCHAR") return "\"" + val + "\"";
        if (t == "DATETIME")
        {
            DateTime date = DateTime.Parse(val);

            return "\"" + date.ToString("yyyy-MM-dd HH:mm:ss") + "\"";

        }

        return val;
    }
    */
}
