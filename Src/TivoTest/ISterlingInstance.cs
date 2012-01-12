using Wintellect.Sterling;

namespace TivoTest
{
    public interface ISterlingInstance
    {
        void Activate();
        void Deactivate();

        ISterlingDatabaseInstance Database { get; }
    }
}
