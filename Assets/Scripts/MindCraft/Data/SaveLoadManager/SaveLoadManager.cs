using System.IO;
using MindCraft.Model;
using UnityEngine;
using BinaryReader = MindCraft.Common.Serialization.BinaryReader;
using BinaryWriter = MindCraft.Common.Serialization.BinaryWriter;

public interface ISaveLoadManager
{
    void SaveGame();
    void LoadGame();
}

public class SaveLoadManager : ISaveLoadManager
{
    [Inject] public IWorldModel WorldModel { get; set; }

    public const string SAVE_FILE_NAME = "/savedGame.dat";
    public const string SAVE_FILE_RES_NAME = "/savedGame.bytes";

    public void SaveGame()
    {
        var writer = new BinaryWriter();
        WorldModel.Serialize(writer);

        var path = Application.persistentDataPath + SAVE_FILE_NAME;

        if (File.Exists(Application.persistentDataPath + SAVE_FILE_NAME))
            File.Delete(path);

        FileStream file = File.Create(Application.persistentDataPath + SAVE_FILE_NAME);
        file.Close();

        var saveBytes = writer.GetWriteBuffer();
        File.WriteAllBytes(path, saveBytes);
    }

    public void LoadGame()
    {
        byte[] bytes = null;
        string path = Application.persistentDataPath + SAVE_FILE_NAME;
        if (File.Exists(path))
        {
            bytes = File.ReadAllBytes(path);
        }
        else
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(SAVE_FILE_RES_NAME);
            TextAsset saveAsset = Resources.Load(fileNameWithoutExtension) as TextAsset;
            if(saveAsset != null)
                bytes = saveAsset.bytes;
        }

        if (bytes == null)
            return;
        
        var reader = new BinaryReader(bytes, 0);
        WorldModel.Deserialize(reader);
    }
}