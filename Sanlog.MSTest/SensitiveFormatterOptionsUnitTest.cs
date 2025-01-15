using System.ComponentModel;

namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class SensitiveFormatterOptionsUnitTest
    {
        [TestMethod]
        public void AddSensitiveSingle()
        {
            var options = new SensitiveFormatterOptions();
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.AddSensitive(SensitiveKeyType.SegmentName, property: null!));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.AddSensitive((SensitiveKeyType)int.MaxValue, "Password"));
            Assert.IsTrue(options.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            options.MakeReadOnly();
            _ = Assert.ThrowsException<InvalidOperationException>(() => options.AddSensitive(SensitiveKeyType.SegmentName, "ClientPassword"));
        }
        [TestMethod]
        public void AddSensitiveCollection()
        {
            var options = new SensitiveFormatterOptions();
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.AddSensitive(SensitiveKeyType.SegmentName, args: null!));
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.AddSensitive(SensitiveKeyType.SegmentName, "Password", null!));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.AddSensitive((SensitiveKeyType)int.MaxValue, "Password", "ClientPassword"));
            Assert.AreEqual(3, options.AddSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword", "GrantType"));
            Assert.AreEqual(1, options.AddSensitive(SensitiveKeyType.SegmentName, "Password", "Username")); // Password is added before
            options.MakeReadOnly();
            _ = Assert.ThrowsException<InvalidOperationException>(() => options.AddSensitive(SensitiveKeyType.SegmentName, "ClientId"));
        }
        [TestMethod]
        public void Clear()
        {
            var options = new SensitiveFormatterOptions();
            Assert.IsTrue(options.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            options.Clear();
            Assert.IsFalse(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            options.MakeReadOnly();
            _ = Assert.ThrowsException<InvalidOperationException>(options.Clear);
        }
        [TestMethod]
        public void IsSensitive()
        {
            var options = new SensitiveFormatterOptions();
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.IsSensitive(SensitiveKeyType.SegmentName, property: null!));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.IsSensitive((SensitiveKeyType)int.MaxValue, "Password"));
            Assert.IsTrue(options.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsFalse(options.IsSensitive(SensitiveKeyType.SegmentName, "ClientPassword"));
        }
        [TestMethod]
        public void CopyTo()
        {
            var original = new SensitiveFormatterOptions();
            Assert.AreEqual(3, original.AddSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword", "GrantType"));
            var copy = new SensitiveFormatterOptions();
            Assert.IsTrue(copy.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsFalse(copy.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            _ = Assert.ThrowsException<ArgumentNullException>(() => original.CopyTo(null!));
            Assert.AreEqual(2, original.CopyTo(copy));
            copy.MakeReadOnly();
            _ = Assert.ThrowsException<InvalidOperationException>(() => original.CopyTo(copy));
        }
        [TestMethod]
        public void MakeReadOnly()
        {
            var options = new SensitiveFormatterOptions();
            Assert.IsFalse(options.IsReadOnly);
            options.MakeReadOnly();
            Assert.IsTrue(options.IsReadOnly);
        }
        [TestMethod]
        public void RemoveSensitiveType()
        {
            var options = new SensitiveFormatterOptions();
            Assert.AreEqual(3, options.AddSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword", "GrantType"));
            Assert.AreEqual(2, options.AddSensitive(SensitiveKeyType.DictionaryEntry, "Data", "Code"));
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.RemoveSensitive((SensitiveKeyType)int.MaxValue));
            Assert.IsTrue(options.RemoveSensitive(SensitiveKeyType.SegmentName));
            Assert.IsFalse(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsFalse(options.RemoveSensitive(SensitiveKeyType.SegmentName)); // removed before
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.DictionaryEntry, "Data"));
            options.MakeReadOnly();
            _ = Assert.ThrowsException<InvalidOperationException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName));
        }
        [TestMethod]
        public void RemoveSensitiveSingle()
        {
            var options = new SensitiveFormatterOptions();
            Assert.AreEqual(2, options.AddSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword"));
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName, property: null!));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.RemoveSensitive((SensitiveKeyType)int.MaxValue, "Password"));
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsTrue(options.RemoveSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsFalse(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsFalse(options.RemoveSensitive(SensitiveKeyType.SegmentName, "Password"));
            options.MakeReadOnly();
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "ClientPassword"));
            _ = Assert.ThrowsException<InvalidOperationException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName, "ClientPassword"));
        }
        [TestMethod]
        public void RemoveSensitiveCollection()
        {
            var options = new SensitiveFormatterOptions();
            Assert.AreEqual(3, options.AddSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword", "GrantType"));
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName, args: null!));
            _ = Assert.ThrowsException<ArgumentNullException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName, "ClientPassword", null!));
            _ = Assert.ThrowsException<InvalidEnumArgumentException>(() => options.RemoveSensitive((SensitiveKeyType)int.MaxValue, "ClientPassword", "GrantType"));
            Assert.AreEqual(2, options.RemoveSensitive(SensitiveKeyType.SegmentName, "ClientPassword", "GrantType"));
            Assert.AreEqual(0, options.RemoveSensitive(SensitiveKeyType.SegmentName, "ClientPassword", "GrantType")); // removed before
            options.MakeReadOnly();
            Assert.IsTrue(options.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            _ = Assert.ThrowsException<InvalidOperationException>(() => options.RemoveSensitive(SensitiveKeyType.SegmentName, "Password", "ClientPassword"));
        }
    }
}