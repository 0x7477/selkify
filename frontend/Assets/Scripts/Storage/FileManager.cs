using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public static class FileManager
{
    public static async Task<string> Read(string path)
    {
        return await File.ReadAllTextAsync(path, Encoding.UTF8);
    }

    public static async Task Write(string path, string content)
    {
        await File.WriteAllTextAsync(path,content);
    }
    
}
