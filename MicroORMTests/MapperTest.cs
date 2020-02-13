using DanilovSoft.MicroORM;
using DanilovSoft.MicroORM.ObjectMapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    [TestClass]
    public class MapperTest
    {
        [TestMethod]
        public void TestList()
        {
            typeof(TestStruct).rea
            var toObject = new ObjectMapper<TestStruct>();
            return toObject.ReadObject();
        }
    }
}
