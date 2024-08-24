using NpmPackChecker.WUI.Dto;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NpmPackChecker.WUI.Services;

public enum NpmChekType
{
    Current,
    Default
}

public class NpmRegService
{
    private readonly IHttpClientFactory _factory;

    //private readonly BaseAppSettings _bas;

    public NpmRegService(IHttpClientFactory factory)//, DataStorageService dataStorage
    {
        _factory = factory;

        //_bas = dataStorage.GetByKey<BaseAppSettings>("BaseAppSettings");
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();

        //_bas.NpmRegistry = "https://registry.npmjs.org/";
        //_bas.NpmRegistry = "http://proxyp.dmzp.local/dmzart1/repository/npmjs/";

        client.BaseAddress = new Uri("https://registry.npmjs.org/");

        return client;
    }
    private HttpClient CreateClientDefault()
    {
        var client = _factory.CreateClient();
        //_bas.NpmRegistry = "https://registry.npmjs.org/";
        client.BaseAddress = new Uri("https://registry.npmjs.org/");
        return client;
    }

    public async Task<PackDetailDto?> GetPackInfoBase(string packName, NpmChekType type = NpmChekType.Current)
    {
        try
        {
            var cl = type == NpmChekType.Default ? CreateClientDefault() : CreateClient();

            HttpRequestMessage request = new(HttpMethod.Get, $"{packName}");

            var response = await cl.SendAsync(request);
            var contentStr = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<PackDetailDto>(contentStr);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    //public async Task<PackDetailDto?> GetPackInfo(string packName)
    //{
    //    try
    //    {
    //        var checks = new List<string>() { "string_decoder", "readable-stream", "ieee754" };
    //        if (checks.Contains(packName))
    //            return null;

    //        var cl = CreateClient();

    //        var url = $"{packName}";

    //        HttpRequestMessage request = new(HttpMethod.Get, url);

    //        var response = await cl.SendAsync(request);
    //        var contentStr = await response.Content.ReadAsStringAsync();

    //        if (!response.IsSuccessStatusCode)
    //            return null;

    //        return JsonSerializer.Deserialize<PackDetailDto>(contentStr);
    //    }
    //    catch (Exception)
    //    {
    //        return null;
    //    }
    //}
    //public async Task<PackDetailDto?> GetPackInfoDefault(string packName)
    //{
    //    var cl = CreateClientDefault();

    //    var url = $"{packName}";

    //    HttpRequestMessage request = new(HttpMethod.Get, url);

    //    var response = await cl.SendAsync(request);
    //    var contentStr = await response.Content.ReadAsStringAsync();

    //    if (!response.IsSuccessStatusCode)
    //        return null;

    //    return JsonSerializer.Deserialize<PackDetailDto>(contentStr);
    //}

    public async Task<bool> CheckPackage(string tarballUrl)
    {
        try
        {
            var client = _factory.CreateClient();

            HttpRequestMessage request = new(HttpMethod.Get, tarballUrl);

            var response = await client.SendAsync(request);
            var contentStr = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"{contentStr}");

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
