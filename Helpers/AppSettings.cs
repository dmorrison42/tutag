namespace Tutag.Helpers
{
    public class AppSettings
    {
        static AppSettings() {
            _secret = System.Guid.NewGuid().ToString("B").ToUpper();
        }

        private static string _secret;
        public string Secret => _secret;
    }
}