using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RoboSharp.Extensions.UnitTests
{
    [TestClass]
    public class Issue178
    {
        [TestMethod]
        public void TestIssue()
        {
            try
            {
                using (var cmd = new RoboCommand())
                {
                }
            }
            catch
            {
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                Thread.Sleep(10000);
            }
        }
    }
}
