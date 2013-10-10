using System;

namespace Tivo.Connect
{
    public interface IMindRpcHeaderInfo
    {
        string ApplicationName { get; }
        Version ApplicationVersion { get; }
        int SchemaVersion { get; }
    }
}
