using System;
using UnityEngine;

namespace Xilo.Core
{
    public static class XiloLog
    {
        const string Prefix = "[XiloVR]";

        public static void Info(string category, string message, UnityEngine.Object context = null)
        {
            Debug.Log($"{Prefix}[{category}] {message}", context);
        }

        public static void Warn(string category, string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning($"{Prefix}[{category}] {message}", context);
        }

        public static void Error(string category, string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"{Prefix}[{category}] {message}", context);
        }

        public static void Exception(string category, Exception exception, UnityEngine.Object context = null)
        {
            Debug.LogError($"{Prefix}[{category}] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}", context);
        }
    }
}
