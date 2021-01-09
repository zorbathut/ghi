namespace Ghi.Test
{
    using NUnit.Framework;
    using System;

    public class SimpleComponent
    {
        public int number;
    }

    public class StringComponent
    {
        public string str;
    }

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            // we turn on error handling so that Clear can work even if we're in the wrong mode
            handlingErrors = true;

            Ghi.Environment.Clear();

            Dec.Database.Clear();

            handlingWarnings = false;
            handledWarning = false;

            handlingErrors = false;
            handledError = false;

            Dec.Config.UsingNamespaces = new string[] { "Ghi", "Ghi.Test", TestContext.CurrentContext.Test.ClassName };
        }

        private bool handlingWarnings = false;
        private bool handledWarning = false;

        private bool handlingErrors = false;
        private bool handledError = false;

        [OneTimeSetUp]
        public void PrepHooks()
        {
            Dec.Config.WarningHandler = str => {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingWarnings)
                {
                    handledWarning = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures
                    throw new ArgumentException(str);
                }
            };

            Dec.Config.ErrorHandler = str => {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingErrors)
                {
                    // If we're handling it, don't throw - this way we can validate that fallback behavior is working right
                    handledError = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures and can validate that exception-passing behavior is working right
                    throw new ArgumentException(str);
                }
            };

            Dec.Config.ExceptionHandler = e => {
                Dec.Config.ErrorHandler(e.ToString());
            };

            Dec.Config.UsingNamespaces = new string[] { "Ghi", "Ghi.Test" };
        }

        protected void ExpectWarnings(Action action)
        {
            Assert.IsFalse(handlingWarnings);
            handlingWarnings = true;
            handledWarning = false;

            action();

            Assert.IsTrue(handlingWarnings);
            Assert.IsTrue(handledWarning);
            handlingWarnings = false;
            handledWarning = false;
        }

        protected void ExpectErrors(Action action)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError);
            handlingErrors = false;
            handledError = false;
        }

        protected void ExpectException(Action action)
        {
            bool excepted = false;
            try
            {
                action();
            }
            catch
            {
                excepted = true;
            }

            Assert.IsTrue(excepted);
        }
    }
}
