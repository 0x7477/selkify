using System;
using MySqlConnector;
using Newtonsoft.Json;
public class DB
{

    public static async Task<MySqlConnection> getOpenConnection()
    {
        var c = new MySqlConnection(Settings.dbconn);
        await c.OpenAsync();
        return c;
    }

    public static async Task<MySqlCommand> getCommand()
    {
        return (await getOpenConnection()).CreateCommand();
    }

    public static async Task<MySqlCommand> getCommand(string sql)
    {
        var cmd = await getCommand();
        cmd.CommandText = sql;
        return cmd;
    }



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
}
