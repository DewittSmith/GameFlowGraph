namespace GameFlow
{
    public interface IInjectData
    {
        public static IInjectData Empty { get; } = new EmptyData();

        T Get<T>(string name = null);

        private class EmptyData : IInjectData
        {
            public T Get<T>(string name = null)
            {
                return default;
            }
        }
    }
}