namespace WebApplication1.Replay
{
    public sealed class Helper
    {
        private static readonly IMusicProvider neteaseProvider = new NetEaseProvider();
        private static readonly IMusicProvider xiamiProvider = new XiaMiProvider();
        private static readonly IMusicProvider qqProvider = new QQProvider();
        public static IMusicProvider GetProviderByName(string source)
        {
            IMusicProvider provider = null;
            switch (source)
            {
                case "netease":
                    provider = neteaseProvider;
                    break;
                case "xiami":
                    provider = xiamiProvider;
                    break;
                case "qq":
                    provider = qqProvider;
                    break;
                default:
                    break;
            }
            return provider;
        }
        public static IMusicProvider GetProvider(string itemId)
        {
            string providerItem = itemId.Split('_')[0];
            if (providerItem.StartsWith("ne"))
                return neteaseProvider;
            if (providerItem.StartsWith("xm"))
                return xiamiProvider;
            if (providerItem.StartsWith("qq"))
                return qqProvider;
            return null;
        }
        public static IMusicProvider[] GetProviderList() => new IMusicProvider[] { neteaseProvider, xiamiProvider, qqProvider };
    }
}
