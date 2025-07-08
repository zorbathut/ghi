using Dec;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Ghi.Test
{
    public class SimpleComponent : IRecordable
    {
        public int number;

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref number, "number");
        }
    }

    public class StringComponent : IRecordable
    {
        public string str;

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref str, "str");
        }
    }

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            // stop verifying things
            errorValidator = null;
            warningValidator = null;

            // we turn on error handling so that Clear can work even if we're in the wrong mode
            handlingErrors = true;

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
        private Func<string, bool> errorValidator = null;
        private Func<string, bool> warningValidator = null;

        public enum EnvironmentMode
        {
            Standard,
            ReadWrite,
            Cloned,
        }

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

        public static void UpdateTestParameters(Dec.Config.UnitTestParameters parameters)
        {
            typeof(Dec.Config).GetField("TestParameters", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, parameters);
        }

        protected enum ExpectationType
        {
            Disallow,
            Tolerate,
            Expect,
        }
        private bool withinExpect = false;
        protected void ExpectGeneral(Action action, string context = "unlabeled context", ExpectationType warning = ExpectationType.Disallow, Func<string, bool> warningValidator = null, ExpectationType error = ExpectationType.Disallow, Func<string, bool> errorValidator = null)
        {
            Assert.IsFalse(withinExpect);
            withinExpect = true;

            // Check initial states based on expectations
            if (warning != ExpectationType.Disallow)
            {
                Assert.IsFalse(handlingWarnings, "Already handling warnings");
                handlingWarnings = true;
                handledWarning = false;
                this.warningValidator = warningValidator;
            }

            if (error != ExpectationType.Disallow)
            {
                Assert.IsFalse(handlingErrors, "Already handling errors");
                handlingErrors = true;
                handledError = false;
                this.errorValidator = errorValidator;
            }

            // Execute the action
            action();

            // Check for expected warnings
            if (warning == ExpectationType.Expect)
            {
                Assert.IsTrue(handlingWarnings);
                Assert.IsTrue(handledWarning, $"Expected warning in {context} but did not generate one");
            }

            // Check for expected errors
            if (error == ExpectationType.Expect)
            {
                Assert.IsTrue(handlingErrors);
                Assert.IsTrue(handledError, $"Expected error in {context} but did not generate one");
            }

            // Reset state for warnings
            if (warning != ExpectationType.Disallow)
            {
                handlingWarnings = false;
                handledWarning = false;
                this.warningValidator = null;
            }

            // Reset state for errors
            if (error != ExpectationType.Disallow)
            {
                handlingErrors = false;
                handledError = false;
                this.errorValidator = null;
            }

            withinExpect = false;
        }

        protected void ExpectWarnings(Action action, string context = "unlabeled context", Func<string, bool> warningValidator = null)
        {
            ExpectGeneral(action, context, ExpectationType.Expect, warningValidator, ExpectationType.Disallow, null);
        }

        // Return "true" if this is the expected error, "false" if this is a bad error
        protected void ExpectErrors(Action action, string context = "unlabeled context", Func<string, bool> errorValidator = null)
        {
            ExpectGeneral(action, context, ExpectationType.Disallow, null, ExpectationType.Expect, errorValidator);
        }

        protected void ExpectWarningsAndErrors(Action action, string context = "unlabeled context",
            Func<string, bool> warningValidator = null, Func<string, bool> errorValidator = null)
        {
            ExpectGeneral(action, context, ExpectationType.Expect, warningValidator, ExpectationType.Expect, errorValidator);
        }

        public void ProcessEnvMode(Ghi.Environment env, EnvironmentMode mode, Action<Ghi.Environment> test)
        {
            switch (mode)
            {
                case EnvironmentMode.Standard:
                    test(env);
                    break;

                case EnvironmentMode.ReadWrite:
                {
                    var envText = Dec.Recorder.Write(env);
                    var envDupe = Dec.Recorder.Read<Ghi.Environment>(envText);
                    using (var scope = new Ghi.Environment.Scope(envDupe))
                    {
                        test(envDupe);
                    }

                    break;
                }

                case EnvironmentMode.Cloned:
                {
                    var envDupe = Dec.Recorder.Clone(env);
                    using (var scope = new Ghi.Environment.Scope(envDupe))
                    {
                        test(envDupe);
                    }

                    break;
                }
            }
        }
    }
}
