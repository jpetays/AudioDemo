using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg
{
    public static class MyAssert
    {
        [Conditional("PRG_ASSERT")]
        public static void AreEqual<T>(T expected, T actual, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            var areEqual = EqualityComparer<T>.Default.Equals(expected, actual);
            if (areEqual)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Assertion failed";
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("AreEqual")} expected '{expected}' != actual '{actual}': {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.AreEqual(expected, actual, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void AreNotEqual<T>(T expected, T actual, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            var areEqual = EqualityComparer<T>.Default.Equals(expected, actual);
            if (!areEqual)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Assertion failed";
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("AreNotEqual")} expected '{expected}' == actual '{actual}': {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.AreNotEqual(expected, actual, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void IsNull<T>(T value, string message, Object context,
            [CallerMemberName] string memberName = null) where T : class
        {
            var isNull = value == null;
            if (isNull)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsNull")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsNull(value, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void IsNull(Object value, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            var isNull = value == null;
            if (isNull)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsNull")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsNull(value, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void IsNotNull<T>(T value, string message, Object context,
            [CallerMemberName] string memberName = null) where T : class
        {
            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                IsNotNull((object)value as Object, message, context);
                return;
            }
            var isNull = value == null;
            if (!isNull)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsNotNull")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsNotNull(value, message);
        }

        [Conditional("PRG_ASSERT")]
        private static void IsNotNull(Object value, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            var isNull = value == null;
            if (!isNull)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsNotNull")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsNotNull(value, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void IsTrue(bool condition, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            if (condition)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsTrue")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsTrue(condition, message);
        }

        [Conditional("PRG_ASSERT")]
        public static void IsFalse(bool condition, string message, Object context,
            [CallerMemberName] string memberName = null)
        {
            if (!condition)
            {
                return;
            }
            Debug.FormatMessage(LogType.Error,
                $"{RichText.Red("IsFalse")}: {message}",
                context, memberName, new StackFrame(1).GetMethod());
            Assert.IsFalse(condition, message);
        }
    }
}
