using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wolffun.StorageResource
{
    internal struct StorageCachedMetaData
    {
        internal Dictionary<string, StorageCachedMetaDataModel> dicLinkDownloaded;

        private const string CACHED_FILE_NAME = "/storageResourceCacheMetaData.dat";

        private string fullFilePath;
        
        
        internal void Init(string saveFileFolderPath)
        {
            fullFilePath = Application.persistentDataPath + saveFileFolderPath + CACHED_FILE_NAME;
            LoadCached(fullFilePath);
        }

        private void SaveCache()
        {
            LocalFileManager.WriteDataToLocalFile(fullFilePath, dicLinkDownloaded);
        }

        private void LoadCached(string fileLocation)
        {
            dicLinkDownloaded = LocalFileManager.ReadDataFromLocalFile<Dictionary<string, StorageCachedMetaDataModel>>(fileLocation);

            if (dicLinkDownloaded == default)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();
        }

        internal void MarkFileUrlDownloaded(string fileUrl)
        {
            if (dicLinkDownloaded == null)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();
            
            if (dicLinkDownloaded.TryGetValue(fileUrl, out var localData))
                return;

            dicLinkDownloaded[fileUrl] = new StorageCachedMetaDataModel()
            {
                url = fileUrl,
            };
            
            SaveCache();
        }

        internal void MarkFileUrlInvalid(string fileUrl)
        {
            if (dicLinkDownloaded == null)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();

            dicLinkDownloaded.Remove(fileUrl);
            
            SaveCache();
        }

        internal bool IsFileDownloaded(string url)
        {
            if (dicLinkDownloaded == null)
                return false;

            return dicLinkDownloaded.ContainsKey(url);
        }
    }

    internal struct StorageCachedMetaDataModel
    {
        internal string url;
    }
}