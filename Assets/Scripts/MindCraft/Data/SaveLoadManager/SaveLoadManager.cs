using System.IO;
using MindCraft.Controller;
using MindCraft.Model;
using UnityEngine;
using BinaryReader = MindCraft.Common.Serialization.BinaryReader;
using BinaryWriter = MindCraft.Common.Serialization.BinaryWriter;

public interface ISaveLoadManager
{
    void SaveGame();
    void LoadGame();
    LoadedGame LoadedGame { get; }
}

public struct LoadedGame
{
    public bool IsLoaded;
    public Vector3 InitPosition { get; }
    public float Yaw { get; }
    public float CameraPitch { get; }

    public LoadedGame(Vector3 initPosition, float yaw, float cameraPitch)
    {
        IsLoaded = true;
        InitPosition = initPosition;
        Yaw = yaw;
        CameraPitch = cameraPitch;
    }
}

public class SaveLoadManager : ISaveLoadManager
{
    [Inject] public IPlayerController PlayerController { get; set; }
    [Inject] public IWorldModel WorldModel { get; set; }

    public const string SAVE_FILE_NAME = "/savedGame.dat";
    public const string SAVE_FILE_RES_NAME = "/savedGame.bytes";

    public LoadedGame LoadedGame { get; private set; }

    public void SaveGame()
    {
        var writer = new BinaryWriter();
        writer.Begin();
        writer.Write(PlayerController.PlayerPosition);
        writer.Write(PlayerController.Yaw);
        writer.Write(PlayerController.CameraPitch);
        writer.Write(WorldModel);
        WorldModel.Serialize(writer);
        writer.End();

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
            if (saveAsset != null)
                bytes = saveAsset.bytes;
        }

        if (bytes == null)
            return;

        var reader = new BinaryReader(bytes, 0);
        var initPosition = reader.ReadVector3();
        var yaw = reader.ReadFloat();
        var cameraPitch = reader.ReadFloat();
        WorldModel.Deserialize(reader);

        LoadedGame = new LoadedGame(initPosition, yaw, cameraPitch);
    }
}