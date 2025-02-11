using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NpmPackChecker.WUI.Dto;

public class PackDetailResult
{
    public PackDetailDto Data { get; set; }
    public string Error { get; set; }

    public static PackDetailResult Success(PackDetailDto data)
    {
        return new PackDetailResult()
        {
            Data = data,
            Error = null
        };
    }

    public static PackDetailResult Fail(string error)
    {
        return new PackDetailResult()
        {
            Data = null,
            Error = error
        };
    }
}

public class PackDetailDto
{
    [JsonPropertyName("dist-tags")]
    public DistTagsDto DistTags { get; set; }

    [JsonPropertyName("versions")]
    public Dictionary<string, VersionDto> Versions { get; set; }

    [JsonPropertyName("time")]
    public Dictionary<string, DateTime> Time { get; set; }

    public PackDetailDto()
    {
        Versions = new();
        Time = new();
    }
}

public class DistTagsDto
{
    [JsonPropertyName("latest")]
    public string Latest { get; set; }

    [JsonPropertyName("next")]
    public string Next { get; set; }

    [JsonPropertyName("previous")]
    public string Previous { get; set; }
}

public class VersionDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; }

    [JsonPropertyName("dist")]
    public DistDto Dist { get; set; }

    public VersionDto()
    {
        Dependencies = new();
    }
}

public class DistDto
{
    [JsonPropertyName("tarball")]
    public string Tarball { get; set; }
}
