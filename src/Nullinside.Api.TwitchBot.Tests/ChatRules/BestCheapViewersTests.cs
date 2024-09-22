namespace Nullinside.Api.TwitchBot.Tests.ChatRules;

public class BestCheapViewersTests
{
    [Test]
    [TestCase("Best viewers on ***")]
    [TestCase("Best  viewers on ***")]
    [TestCase("Best vie̮wers on ***")]
    [TestCase("Best́ viewers on ***")]
    [TestCase("Be̩st Viewers on ***")]
    [TestCase("Be̾st Viewers on ***")]
    [TestCase("B͟est Viewers on ***")]
    [TestCase("B̟est viewers on ***")]
    [TestCase("Cheap viewers on ***")]
    [TestCase("Che̢ap vie̮wers on ***")]
    [TestCase("Ch̍eap Viewers on ***")]
    [TestCase("Ch͟eap viewers on ***")]
    [TestCase("C̀heap Viewers on ***")]
    [TestCase("Cheaͧp v̫iewers on ***")]
    [TestCase("Cheaͧp v̫iewers on *** ")]
    public void TestKnownStrings(string badString)
    {
        // Need to put interfaces in front of the classes before we can do this.
        Assert.Pass();
    }
}