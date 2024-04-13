namespace Nova;

public interface IStateObject
{
    void Sync();
    void SyncImmediate();
}
