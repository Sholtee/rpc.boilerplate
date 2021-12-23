namespace Services.API
{
    public interface IConfig<TNode>
    {
        TNode Value { get; }
    }
}
