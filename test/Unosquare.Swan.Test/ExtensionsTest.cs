﻿namespace Unosquare.Swan.Test.ExtensionsTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using Networking;
    using Mocks;

    [TestFixture]
    public class Benchmark : TestFixtureBase
    {
        [Test]
        public void WithAction_ReturnsTimeSpan()
        {
            const int total = 0;
            var action = new Action(() =>
            {
                if (total < 2)
                    throw new Exception();
            });

            var result = action.Benchmark();

            Assert.IsNotNull(result);
        }

        [Test]
        public void WithEmptyAction_ReturnsTimeSpan()
        {
            var action = new Action(() => { });

            var result = action.Benchmark();

            Assert.IsNotNull(result);
        }

        [Test]
        public void WithNullAction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NullAction.Benchmark());
        }
    }

    [TestFixture]
    public class Retry : TestFixtureBase
    {
        [Test]
        public void WithNewFunction_RetryAction()
        {
            var total = 0;

            var action = new Func<int>(() =>
            {
                if (total++ < 2)
                    throw new Exception();

                return total;
            });

            var result = action.Retry();
            Assert.AreEqual(3, result);
        }

        [Test]
        public void WithInvalidAction_ThrowsAggregateException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var action =
                    new Action(() => JsonClient.GetString("http://accesscore.azurewebsites.net/api/token").Wait());

                action.Retry();
            });
        }

        [Test]
        public void WithValidAction_DoesNotThrowException()
        {
            var total = 0;

            Assert.DoesNotThrow(() =>
            {
                var action = new Action(() =>
                {
                    if (total++ < 2)
                        throw new Exception();
                });

                action.Retry();
            });
        }

        [Test]
        public void WithNullAction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NullAction.Retry());
        }

        [Test]
        public void WithNullFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((Func<int>) null).Retry());
        }
    }

    [TestFixture]
    public class CopyPropertiesTo
    {
        [Test]
        public void WithValidObjectAttr_CopyPropertiesToTarget()
        {
            var source = ObjectAttr.GetDefault();
            var target = new ObjectAttr();

            source.CopyPropertiesTo(target);

            Assert.AreEqual(source.Name, target.Name);
            Assert.AreEqual(source.IsActive, target.IsActive);
        }

        [Test]
        public void WithValidBasicJson_CopyPropertiesToTarget()
        {
            var source = BasicJson.GetDefault();
            var destination = new BasicJson();

            source.CopyPropertiesTo(destination);

            Assert.AreEqual(source.BoolData, destination.BoolData);
            Assert.AreEqual(source.DecimalData, destination.DecimalData);
            Assert.AreEqual(source.StringData, destination.StringData);
            Assert.AreEqual(source.StringNull, destination.StringNull);
        }

        [Test]
        public void WithNullObjectAttr_CopyPropertiesToTarget()
        {
            Assert.Throws<ArgumentNullException>(() => ObjectAttr.GetDefault().CopyPropertiesTo(null));
        }

        [Test]
        public void WithValidParamsAndNewProperty_CopyPropertiesToTarget()
        {
            var source = BasicJson.GetDefault();
            source.StringNull = "1";

            var destination = new BasicJsonWithNewProperty();

            source.CopyPropertiesTo(destination);

            Assert.AreEqual(source.BoolData, destination.BoolData);
            Assert.AreEqual(source.DecimalData, destination.DecimalData);
            Assert.AreEqual(source.StringData, destination.StringData);
            Assert.AreEqual(source.StringNull, destination.StringNull.ToString());
        }

        [Test]
        public void WithValidBasicJson_CopyNotIgnoredPropertiesToTarget()
        {
            var source = BasicJson.GetDefault();
            var destination = new BasicJson();

            source.CopyPropertiesTo(destination, new[] {nameof(BasicJson.NegativeInt), nameof(BasicJson.BoolData)});

            Assert.AreNotEqual(source.BoolData, destination.BoolData);
            Assert.AreNotEqual(source.NegativeInt, destination.NegativeInt);
            Assert.AreEqual(source.StringData, destination.StringData);
        }

        [Test]
        public void WithValidDictionary_CopyPropertiesToTarget()
        {
            var source = new Dictionary<string, object>
            {
                {nameof(UserDto.Name), "Thrall"},
                {nameof(UserDto.Email), "Warchief.Thrall@horde.com"},
                {nameof(UserDto.Role), "Warchief"}
            };

            var target = new UserDto();

            source.CopyKeyValuePairTo(target);

            Assert.AreEqual(source[nameof(UserDto.Name)].ToString(), target.Name);
            Assert.AreEqual(source[nameof(UserDto.Email)], target.Email);
        }
    }

    [TestFixture]
    public class CopyPropertiesToNew
    {
        [Test]
        public void WithObjectWithCopyableAttribute_CopyPropertiesToNewObjectAttr()
        {
            var source = ObjectAttr.GetDefault();

            var destination = source.CopyPropertiesToNew<ObjectAttr>();

            Assert.IsNotNull(destination);
            Assert.AreSame(source.GetType(), destination.GetType());
            Assert.AreNotEqual(source.Id, destination.Id);
            Assert.AreEqual(source.Name, destination.Name);
            Assert.AreEqual(source.IsActive, destination.IsActive);
        }

        [Test]
        public void WithValidParams_CopyPropertiesToNewObject()
        {
            var source = new ObjectEnum
            {
                Id = 1,
                MyEnum = MyEnum.Two
            };

            var result = source.CopyPropertiesToNew<ObjectEnum>();
            Assert.AreEqual(source.MyEnum, result.MyEnum);
        }

        [Test]
        public void WithValidBasicJson_CopyPropertiesToNewBasicJson()
        {
            var source = BasicJson.GetDefault();
            var destination = source.CopyPropertiesToNew<BasicJson>();

            Assert.IsNotNull(destination);
            Assert.AreSame(source.GetType(), destination.GetType());
            Assert.AreEqual(source.BoolData, destination.BoolData);
            Assert.AreEqual(source.DecimalData, destination.DecimalData);
            Assert.AreEqual(source.StringData, destination.StringData);
            Assert.AreEqual(source.StringNull, destination.StringNull);
        }

        [Test]
        public void WithNullSource_ThrowsArgumentNullException()
        {
            ObjectEnum source = null;

            Assert.Throws<ArgumentNullException>(() => source.CopyPropertiesToNew<ObjectEnum>());
        }
    }

    [TestFixture]
    public class DeepClone
    {
        [Test]
        public void WithSmtpState_CloneProperly()
        {
            var sampleBuffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            var source = new SmtpSessionState();
            source.DataBuffer.AddRange(sampleBuffer);

            var target = source.DeepClone();

            Assert.AreEqual(source.DataBuffer.Count, target.DataBuffer.Count);
            source.ResetEmail();

            Assert.AreEqual(0, source.DataBuffer.Count);
            Assert.AreEqual(sampleBuffer.Length, target.DataBuffer.Count);
        }
    }

    [TestFixture]
    public class CopyOnlyPropertiesTo
    {
        [Test]
        public void WithValidBasicJson_CopyOnlyPropertiesToTarget()
        {
            var source = BasicJson.GetDefault();
            var destination = new BasicJson {NegativeInt = 800, BoolData = false};
            source.CopyOnlyPropertiesTo(destination, new[] {nameof(BasicJson.NegativeInt), nameof(BasicJson.BoolData)});

            Assert.AreEqual(source.BoolData, destination.BoolData);
            Assert.AreEqual(source.NegativeInt, destination.NegativeInt);
            Assert.AreNotEqual(source.StringData, destination.StringData);
        }

        [Test]
        public void WithValidObjectAttr_CopyOnlyPropertiesToTarget()
        {
            var source = ObjectAttr.GetDefault();
            var target = new ObjectAttr();

            source.CopyOnlyPropertiesTo(target);

            Assert.AreEqual(source.Name, target.Name);
            Assert.AreEqual(source.IsActive, target.IsActive);
        }
    }

    [TestFixture]
    public class CopyOnlyPropertiesToNew : TestFixtureBase
    {
        [Test]
        public void WithValidParams_CopyOnlyPropertiesToNewObject()
        {
            var source = ObjectAttr.GetDefault();
            var target = source.CopyOnlyPropertiesToNew<ObjectAttr>(new[] {nameof(ObjectAttr.Name)});
            Assert.AreEqual(source.Name, target.Name);
        }

        [Test]
        public void WithNullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                NullObj.CopyOnlyPropertiesToNew<ObjectAttr>(new[] {nameof(ObjectAttr.Name)}));
        }

        [Test]
        public void WithValidBasicJson_CopyOnlyPropertiesToNewBasicJson()
        {
            var source = BasicJson.GetDefault();
            var destination = source.CopyOnlyPropertiesToNew<BasicJson>(new[]
                {nameof(BasicJson.BoolData), nameof(BasicJson.DecimalData)});

            Assert.IsNotNull(destination);
            Assert.AreSame(source.GetType(), destination.GetType());

            Assert.AreEqual(source.BoolData, destination.BoolData);
            Assert.AreEqual(source.DecimalData, destination.DecimalData);
        }
    }

    [TestFixture]
    public class ExceptionMessage : TestFixtureBase
    {
        [Test]
        public void WithNullException_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NullException.ExceptionMessage());
        }

        [Test]
        public void ExceptionMessageTest()
        {
            try
            {
                throw new Exception("Random message");
            }
            catch (Exception ex)
            {
                var msg = ex.ExceptionMessage();
                Assert.IsNotNull(msg);
                Assert.AreEqual(msg, ex.Message);
            }
        }

        [Test]
        public void InnerExceptionTest()
        {
            string[] splits = {"\r\n"};
            var exceptions = new List<Exception>
            {
                new TimeoutException("It timed out", new ArgumentException("ID missing")),
                new NotImplementedException("Somethings not implemented", new ArgumentNullException())
            };

            var ex = new AggregateException(exceptions);

            var msg = ex.ExceptionMessage();
            Assert.IsNotNull(msg);

            var lines = msg.Split(splits, StringSplitOptions.None);
            Assert.AreEqual(lines.Length - 1, ex.InnerExceptions.Count);
        }
    }
}