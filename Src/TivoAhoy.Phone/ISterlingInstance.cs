using Wintellect.Sterling;

namespace TivoAhoy.Phone
{
    public interface ISterlingInstance
    {
        void Activate();
        void Deactivate();

        ISterlingDatabaseInstance Database { get; }
    }
}
