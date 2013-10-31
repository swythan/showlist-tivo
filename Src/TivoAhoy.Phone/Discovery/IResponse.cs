namespace TivoAhoy.Phone.Discovery
{
    using System.IO;

    /// <summary>
    /// Represents the response received from a server or to send to a client
    /// </summary>
    public interface IResponse
    {
    }

    /// <summary>
    /// Represents a response to send to a client
    /// </summary>
    public interface IServerResponse : IResponse
    {
        void WriteTo(Stream writer);
        byte[] GetBytes();
    }

    /// <summary>
    /// Represents a response received from a server
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IClientResponse<TResponse> : IResponse
    {
        TResponse GetResponse(Stream stream);
        TResponse GetResponse(byte[] requestBytes);
    }

}
