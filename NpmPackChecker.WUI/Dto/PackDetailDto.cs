using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NpmPackChecker.WUI.Dto;

public class PackDetailDto
{
    [JsonPropertyName("dist-tags")]
    public DistTagsDto DistTags { get; set; }

    [JsonPropertyName("versions")]
    public Dictionary<string, VersionDto> Versions { get; set; }

    [JsonPropertyName("time")]
    public Dictionary<string, DateTime> Time { get; set; }
}

public class DistTagsDto
{
    [JsonPropertyName("latest")]
    public string Latest { get; set; }
}

public class VersionDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; }

    [JsonPropertyName("dist")]
    public DistDto Dist { get; set; }
}

public class DistDto
{
    [JsonPropertyName("tarball")]
    public string Tarball { get; set; }
}
