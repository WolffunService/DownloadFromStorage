using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wolffun.StorageResource
{
    internal struct StorageCachedMetaData
    {
        internal Dictionary<string, StorageCachedMetaDataModel> dicLinkDownloaded;
        internal LinkedList<string> listLinkDownloaded;

        private const string CACHED_FILE_NAME = "/storageResourceCacheMetaData.dat";
        private const string LIST_CACHED_FILE_NAME = "/storageResourceListCache.dat";

        private string fullFilePath;
        private string listCacheFilePath;
        
        
        internal void Init(string saveFileFolderPath)
        {
            fullFilePath = Application.persistentDataPath + saveFileFolderPath + CACHED_FILE_NAME;
            listCacheFilePath = Application.persistentDataPath + saveFileFolderPath + LIST_CACHED_FILE_NAME;
            LoadCached(fullFilePath);
            LoadListCached(listCacheFilePath);
        }

        internal void SaveCache()
        {
            LocalFileManager.WriteDataToLocalFile(fullFilePath, dicLinkDownloaded);
            LocalFileManager.WriteDataToLocalFile(listCacheFilePath, listLinkDownloaded);
        }

        private void LoadCached(string fileLocation)
        {
            dicLinkDownloaded = LocalFileManager.ReadDataFromLocalFile<Dictionary<string, StorageCachedMetaDataModel>>(fileLocation);

            if (dicLinkDownloaded == default)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();
        }

        private void LoadListCached(string fileLocation)
        {
            listLinkDownloaded = LocalFileManager.ReadDataFromLocalFile<LinkedList<string>>(fileLocation);

            if (listLinkDownloaded == default)
                listLinkDownloaded = new LinkedList<string>();
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

            if (listLinkDownloaded == null)
                listLinkDownloaded = new LinkedList<string>();

            // recently used file will be move to end of list
            listLinkDownloaded.AddLast(fileUrl);
            
            SaveCache();
        }

        internal void MarkFileUrlInvalid(string fileUrl)
        {
            if (dicLinkDownloaded == null)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();
            if (listLinkDownloaded == null)
                listLinkDownloaded = new LinkedList<string>();

            dicLinkDownloaded.Remove(fileUrl);
            listLinkDownloaded.Remove(fileUrl);

            SaveCache();
        }

        internal void MarkFileUrlInvalid(string fileUrl, LinkedListNode<string> node, bool isSaveCachedMetaData = true)
        {
            if (dicLinkDownloaded == null)
                dicLinkDownloaded = new Dictionary<string, StorageCachedMetaDataModel>();
            if (listLinkDownloaded == null)
                listLinkDownloaded = new LinkedList<string>();

            dicLinkDownloaded.Remove(fileUrl);
            try
            {
                listLinkDownloaded.Remove(node); // more efficient
            }
            catch (Exception ex)
            {
                Debug.LogError("MarkFileUrlInvalid throw exception " + ex.Message);
            }

            if (isSaveCachedMetaData)
                SaveCache();
        }

        internal void MarkFileBeingUsed(string url)
        {
            if (!IsFileDownloaded(url))
                return;

            // recently used file will be move to end of list
            var node = listLinkDownloaded.FindLast(url);
            if (node != null)
            {
                try
                {
                    listLinkDownloaded.Remove(node);
                }
                catch (Exception ex)
                {
                    Debug.LogError("MarkFileBeingUsed throw exception " + ex.Message);
                }
            }

            listLinkDownloaded.AddLast(url);

            // I think this is not good when called too many times
            // SaveCache();
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