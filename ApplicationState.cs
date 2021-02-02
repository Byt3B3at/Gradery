namespace Gradery
{
    // http://www.blackwasp.co.uk/Singleton.aspx
    sealed class ApplicationState
    {
        // Singleton Implementation
        private static ApplicationState _instance;
        private static object _lockThis = new object();

        private ApplicationState() { }

        public static ApplicationState GetState()
        {
            lock (_lockThis)
                if (_instance == null)
                    _instance = new ApplicationState();
            return _instance;
        }

        // State Information
        public Student CurrentUser { get; set; }
    }
}
