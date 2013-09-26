namespace TivoAhoy.Common.Discovery
{
    using System.IO;

    public interface IClientRequest
    {
        void WriteTo(Stream stream);
        byte[] GetBytes();
    }

    public interface IClientRequestWriter : IClientRequest
    {
        void WriteTo(BinaryWriter writer);
    }

    public interface IServerRequest<RequestType>
    {
        RequestType GetRequest(Stream stream);
        RequestType GetRequest(byte[] requestBytes);
    }

    public interface IServerRequestReader<TRequest> : IServerRequest<TRequest>
    {
        TRequest GetRequest(BinaryReader writer);
    }
}
