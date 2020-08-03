using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskDefinitionTest {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestSendMail() {
            TaskDefinition.Class1.SendMail("寄件人信箱", "收件人信箱", null, null, null, "測試信(from TaskDefinitionTest)", "無內文");
        }
    }
}
