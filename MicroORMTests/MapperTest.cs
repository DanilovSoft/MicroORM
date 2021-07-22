namespace UnitTests
{
    internal readonly struct TestStruct
    {
        public string Name { get; }

        public int Count { get; }

        public TestStruct(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }

    //public class MapperTest
    //{
    //    [Test]
    //    public void TestList()
    //    {
            
    //        //var toObject = new ObjectMapper<TestStruct>();
    //        //return toObject.ReadObject();
    //    }
    //}
}
