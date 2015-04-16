using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarewolfAgent.Tests
{
    [TestClass]
    public class WarewolfAgentTests
    {
        [TestMethod]
        public void PostDataToWebserverAsRemoteAgentTest()
        {
            Program.PostDataToWebserverAsRemoteAgent(@"Workflow:Examples/Run%20an%20Example%20Build/Event%20Handler%20-%20Run%20me?BuildWorkspace=C%3A%5CBuilds&Email=Ashley.lewis%40dev2.co.za", null, null);
        }
    }
}
