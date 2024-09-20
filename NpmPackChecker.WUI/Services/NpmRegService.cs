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

    public NpmRegService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientRegistry()
    {
        var client = _factory.CreateClient();
        //client.BaseAddress = new Uri("https://registry.npmjs.org/");
        client.BaseAddress = new Uri("http://proxyp.dmzp.local/dmzart1/repository/npmjs/");
        return client;
    }
    private HttpClient CreateClientDefault()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://registry.npmjs.org/");
        return client;
    }

    public async Task<PackDetailDto?> GetPackInfoBase(string packName, NpmChekType type = NpmChekType.Current)
    {
        try
        {
            var cl = type == NpmChekType.Default ? CreateClientDefault() : CreateClientRegistry();

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
