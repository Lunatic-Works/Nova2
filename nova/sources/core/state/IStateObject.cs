namespace Nova;

public interface IStateObject
{
    /// <summary>
    /// Sync with normal
    /// </summary>
    void Sync();
    /// <summary>
    /// Sync during fastward.
    /// </summary>
    void SyncImmediate();
    /// <summary>
    /// Sync during restoration.
    /// </summary>
    void SyncBackend();
}
