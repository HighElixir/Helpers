using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HighElixir.Unity.Addressable
{
    // com.unity.nuget.newtonsoft-json の パッケージ使用
    public class AddressablesDataReader
    {
        public async Task<T> ReadDataAsync<T>(string filePath, IProgress<float> progress)
        {
            //Logging.Log("ReadData, path: " + filePath);
            progress?.Report(0f);
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "Addressables key cannot be null or empty.");

            var handle = Addressables.LoadAssetAsync<TextAsset>(filePath);
            TextAsset textAsset = null;
            try
            {
                progress?.Report(0.5f);
                textAsset = await handle.Task;

                if (textAsset == null)
                {
                    throw new Exception($"[DataReader] Failed to load TextAsset at address: {filePath}");
                }

                if (string.IsNullOrEmpty(textAsset.text))
                {
                    throw new Exception($"[DataReader] TextAsset at {filePath} is empty or null");
                }
                string jsonText = textAsset.text.Replace("\uFEFF", "").Trim(); // BOM除去＆整形
                //Debug.Log(jsonText);
                var result = JsonConvert.DeserializeObject<T>(jsonText);
                if (result == null)
                    throw new JsonSerializationException($"Deserialization of {typeof(T).Name} returned null. Address: {filePath}");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataReader] Error reading data from Addressables\nAddress: {filePath}\nException: {ex}");
                throw;
            }
            finally
            {
                progress?.Report(1f);
                Addressables.Release(handle); // メモリリーク対策！
            }
        }
    }
}
