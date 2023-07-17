using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Wolffun.StorageResource
{
    public static class LocalFileManager
    {
        public static T ReadDataFromLocalFile<T>(string filePath)
        {
            bool isHasFile = false;
#if UNITY_WEBGL
            isHasFile = PlayerPrefs.HasKey(filePath);
#else
            isHasFile = File.Exists(filePath);
#endif

            if (isHasFile)
            {
                try
                {
                    string savedData = string.Empty;

#if UNITY_WEBGL
                    savedData = PlayerPrefs.GetString(dataLink);
#else
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(filePath, FileMode.Open);
                    savedData = (string)bf.Deserialize(file);
                    file.Close();
                    file = null;
#endif
                    T loadedData = JsonConvert.DeserializeObject<T>(savedData);
                    
                    return loadedData;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Load file throw exception " + ex.Message);
                    return default;
                }
            }
            else
            {
                return default;

            }
        }

        public static void WriteDataToLocalFile<T>(string filePath, T data)
        {
            try
            {
                string saveData = JsonConvert.SerializeObject(data);

#if UNITY_WEBGL
                PlayerPrefs.SetString(filePath, saveData);
                PlayerPrefs.Save();
#else
                BinaryFormatter bf = new BinaryFormatter();

                FileStream file = File.Open(filePath, FileMode.OpenOrCreate);
                bf.Serialize(file, saveData);
                file.Close();
                file = null;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError("Save data --" + data.GetType() + "-- is error: " + ex.GetBaseException() + "\n" + ex.StackTrace);
            }
        }

        public static async UniTask DeleteAsync(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("DeleteAsync fail - " + filePath + " - " + ex.Message);
            }
            await UniTask.Yield();
        }

        public static long GetDirectorySizeBytes(string directoryPath)
        {
            long size = 0;
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                size += fileInfo.Length;
            }

            DirectoryInfo[] subDirectoryInfos = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subDirectoryInfo in subDirectoryInfos)
            {
                // Should we avoid using recursive?
                size += GetDirectorySizeBytes(subDirectoryInfo.FullName);
            }

            return size;
        }
    }
}