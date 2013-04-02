using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSettings;

namespace SettingManagerTest
{
    /// <summary>
    ///This is a test class for SettingManagerTest and is intended
    ///to contain all SettingManagerTest Unit Tests
    ///</summary>
    [TestClass]
    public class SettingManagerTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion

        /// <summary>
        ///A test for Save
        ///</summary>
        [TestMethod]
        public void SaveTest()
        {
            var target = new SettingManager();

            target.AddSetting<Int16>("int16");
            target.AddSetting<Int32>("int32");
            target.AddSetting<Int64>("int64");
            target.AddSetting<UInt16>("uint16");
            target.AddSetting<UInt32>("uint32");
            target.AddSetting<UInt64>("uint64");
            target.AddSetting<Boolean>("boolean");
            target.AddSetting<String>("string");
            target.AddSetting<String[]>("string[]");
            target.AddSetting<Byte[]>("byte[]");
            target.AddSetting<List<String>>("list<string>");
            target.AddSetting<List<int>>("list<int>");
            target.AddSetting<object>("object");

            ISettingWriter writer = target;

            writer.ChangeSetting<Int16>("int16", 1);
            writer.ChangeSetting("int32", 2);
            writer.ChangeSetting<Int64>("int64", 3);
            writer.ChangeSetting<UInt16>("uint16", 4);
            writer.ChangeSetting<UInt32>("uint32", 5);
            writer.ChangeSetting<UInt64>("uint64", 6);
            writer.ChangeSetting("boolean", true);
            writer.ChangeSetting("string", "foo");
            writer.ChangeSetting("string[]", new[] {"foo", "bar"});
            writer.ChangeSetting("byte[]", new byte[] {10, 11, 12, 13});
            writer.ChangeSetting("list<string>", new List<String> {"foo", "bar"});
            writer.ChangeSetting("list<int>", new List<int> {1, 2, 3, 4, 5, 6});
            writer.ChangeSetting<object>("object", new List<Boolean>());

            const string registryKey = "Software\\Test\\Foobar";
            target.Save(registryKey);
            Assert.IsTrue(true);
        }

        /// <summary>
        ///A test for Load
        ///</summary>
        [TestMethod]
        public void LoadTest()
        {
            const bool savedTest = true;

            var target = new SettingManager();

            const string registryKey = "Software\\Test\\Foobar";

            target.AddSetting<Int16>("int16");
            target.AddSetting<Int32>("int32");
            target.AddSetting<Int64>("int64");
            target.AddSetting<UInt16>("uint16");
            target.AddSetting<UInt32>("uint32");
            target.AddSetting<UInt64>("uint64");
            target.AddSetting<Boolean>("boolean");
            target.AddSetting<String>("string");
            target.AddSetting<String[]>("string[]");
            target.AddSetting<Byte[]>("byte[]");
            target.AddSetting<List<String>>("list<string>");
            target.AddSetting<List<int>>("list<int>");
            target.AddSetting<object>("object");

            target.Load(registryKey);

            ISettingProvider provider = target;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (savedTest)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                Assert.AreEqual(1, provider.ReadSetting<Int16>("int16"));
                Assert.AreEqual(2, provider.ReadSetting<Int32>("int32"));
                Assert.AreEqual(3, provider.ReadSetting<Int64>("int64"));
                Assert.AreEqual((ushort) 4, provider.ReadSetting<UInt16>("uint16"));
                Assert.AreEqual((ushort) 5, provider.ReadSetting<UInt32>("uint32"));
                Assert.AreEqual((ushort) 6, provider.ReadSetting<UInt64>("uint64"));
                Assert.AreEqual(true, provider.ReadSetting<Boolean>("boolean"));
                Assert.AreEqual("foo", provider.ReadSetting<string>("string"));
                Assert.IsTrue(new[] {"foo", "bar"}.SequenceEqual(provider.ReadSetting<String[]>("string[]")));
                Assert.IsTrue(new byte[] {10, 11, 12, 13}.SequenceEqual(provider.ReadSetting<byte[]>("byte[]")));
                Assert.IsTrue(
                    new List<String> {"foo", "bar"}.SequenceEqual(provider.ReadSetting<List<String>>("list<string>")));
                Assert.IsTrue(
                    new List<int> {1, 2, 3, 4, 5, 6}.SequenceEqual(provider.ReadSetting<List<int>>("list<int>")));
                Assert.IsInstanceOfType(provider.ReadSetting<object>("object"), typeof (List<Boolean>));
            }
            else
// ReSharper disable CSharpWarnings::CS0162
// ReSharper disable HeuristicUnreachableCode
            {
                Assert.AreEqual(default(Int16), provider.ReadSetting<Int16>("int16"));
                Assert.AreEqual(default(Int32), provider.ReadSetting<Int32>("int32"));
                Assert.AreEqual(default(Int64), provider.ReadSetting<Int64>("int64"));
                Assert.AreEqual(default(UInt16), provider.ReadSetting<UInt16>("uint16"));
                Assert.AreEqual(default(UInt32), provider.ReadSetting<UInt32>("uint32"));
                Assert.AreEqual(default(UInt64), provider.ReadSetting<UInt64>("uint64"));
                Assert.AreEqual(default(Boolean), provider.ReadSetting<Boolean>("boolean"));
                Assert.IsNull(provider.ReadSetting<String[]>("string[]"));
                Assert.IsNull(provider.ReadSetting<Byte[]>("byte[]"));
                Assert.IsNull(provider.ReadSetting<List<String>>("list<string>"));
                Assert.IsNull(provider.ReadSetting<List<int>>("list<int>"));
                Assert.IsNull(provider.ReadSetting<object>("object"));
            }
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore CSharpWarnings::CS0162
        }
    }
}