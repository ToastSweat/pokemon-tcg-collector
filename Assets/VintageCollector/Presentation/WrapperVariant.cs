using System.Text;

namespace VintageCollector.Presentation
{
    // Presentation identity only. No pack-generator RNG, file IO or store dependencies.
    public static class WrapperVariant
    {
        public static int ForTransaction(string transactionId)
        {
            if(string.IsNullOrEmpty(transactionId))return 0;
            uint hash=2166136261u;
            foreach(byte value in Encoding.UTF8.GetBytes(transactionId))
                hash=unchecked((hash^value)*16777619u);
            return (int)(hash%3u);
        }
    }
}
