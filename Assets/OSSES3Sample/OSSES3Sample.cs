using System;
using System.Collections;
using System.IO;
using Aliyun.OSS;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OSSES3Sample : MonoBehaviour
{
    private InputField cachedKey;
    private InputField cachedValue;

    private const string CacheFileName = "MyCacheFile.es3";

    private void Start()
    {
        cachedKey = GameObject.Find("InputFieldKey").GetComponent<InputField>();
        cachedValue = GameObject.Find("InputFieldValue").GetComponent<InputField>();

        // 从持久化目录加载缓存文件
        ES3.CacheFile(CacheFileName);
    }

    public void SaveCacheToLocal()
    {
        if (string.IsNullOrEmpty(cachedKey.text))
        {
            Debug.Log("cached key is null or empty!");
            return;
        }

        ES3.Save(cachedKey.text, cachedValue.text, CacheFileName);
        ES3.StoreCachedFile(CacheFileName);
    }

    public void LoadCacheFromLocal()
    {
        if (string.IsNullOrEmpty(cachedKey.text))
        {
            Debug.Log("cached key is null or empty!");
            return;
        }

        var result = ES3.Load(cachedKey.text, CacheFileName, string.Empty);
        cachedValue.text = result;
    }

    public void UploadCacheToOss()
    {
        StartCoroutine(UploadCacheCoroutine());
    }

    private IEnumerator UploadCacheCoroutine()
    {
        // 找运维要自己项目的OSS相关信息
        var authUrl = "http://qa-g-gateway.tope365.com/file/oss/auth/preschool-eggshell";
        var jsonResult = string.Empty;
        using (var req = UnityWebRequest.Get(authUrl))
        {
            req.SetRequestHeader("from", "Y");
            yield return req.SendWebRequest();
            if (req.isHttpError || req.isNetworkError)
            {
                Debug.Log(req.error);
                yield break;
            }

            jsonResult = req.downloadHandler.text;
        }

        var jObject = JObject.Parse(jsonResult);
        var dataObject = jObject["data"];

        // yourEndpoint填写Bucket所在地域对应的Endpoint。以华东1（杭州）为例，Endpoint填写为https://oss-cn-hangzhou.aliyuncs.com。
        var endpoint = "oss-cn-beijing.aliyuncs.com";
        // 阿里云账号AccessKey拥有所有API的访问权限，风险很高。强烈建议您创建并使用RAM用户进行API访问或日常运维，请登录RAM控制台创建RAM用户。
        var accessKeyId = dataObject["accessKeyId"].ToObject<string>();
        var accessKeySecret = dataObject["accessKeySecret"].ToObject<string>();
        var securityToken = dataObject["securityToken"].ToObject<string>();
        // 填写Bucket名称，例如examplebucket。
        var bucketName = "preschool-eggshell";
        // 填写Object完整路径，完整路径中不能包含Bucket名称，例如exampledir/exampleobject.txt。
        var objectName = "eggshell/DevTest/" + CacheFileName;
        // 填写本地文件的完整路径。如果未指定本地路径，则默认从示例程序所属项目对应本地路径中上传文件。
        var localFilename = Path.Combine(Application.persistentDataPath, CacheFileName);

        // 创建OssClient实例。
        var client = new OssClient(endpoint, accessKeyId, accessKeySecret, securityToken);
        PutObjectResult putObjectResult = null;
        try
        {
            // 上传文件。
            putObjectResult = client.PutObject(bucketName, objectName, localFilename);
            print("Put object succeeded");
            print(
                $"putObjectResult.HttpStatusCode:{putObjectResult.HttpStatusCode},{(int)putObjectResult.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            print($"Put object failed, {ex.Message}");
        }
        finally
        {
            putObjectResult?.Dispose();
        }
    }

    public void DownloadCacheFromOss()
    {
        StartCoroutine(DownloadCacheCoroutine());
    }

    private IEnumerator DownloadCacheCoroutine()
    {
        var url = "http://preschool-eggshell.oss-cn-beijing.aliyuncs.com/eggshell/DevTest/" + CacheFileName;
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.isHttpError || req.isNetworkError)
            {
                Debug.Log(req.error);
                yield break;
            }

            var filePath = Path.Combine(Application.persistentDataPath, CacheFileName);
            if (!File.Exists(filePath))
                File.Create(filePath);
            File.WriteAllBytes(filePath, req.downloadHandler.data);
            ES3.CacheFile(CacheFileName);
        }
    }
}