
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib.Patches
{
    public static class PerformanceMonitor
    {
        private static ConcurrentBag<MethodTime> methodTimes = new();

        private static int writeThreshold = 1_000;

        private static string logFilePath = "/home/cat/stationeers_perf.csv";

        private static SemaphoreSlim fileWriteSemaphore = new(1);

        private static ThreadLocal<Stack<StackTraceEntry>> stackTrace = new(()=>new());

        public static void MonitorMethod(
            Harmony harmony,
            MethodInfo method)
        {
            if(typeof(Task).IsAssignableFrom(method.ReturnType)){
                UnityEngine.Debug.LogError("Tasks (async) are not supported. The reported time will be the time to create the task, not to run it");
            }
            harmony.Patch(
                method,
                new HarmonyMethod(
                    typeof(PerformanceMonitor),
                    nameof(MethodStart)
                ),
                new HarmonyMethod(
                    typeof(PerformanceMonitor),
                    nameof(MethodEnd)
                )
            );
        }

        private static void MethodStart(MethodBase __originalMethod){
            stackTrace.Value.Push(new StackTraceEntry(__originalMethod.Name));
        }
        private static void MethodEnd(){
            var stackTraceEntry = stackTrace.Value.Pop();
            var time = stackTraceEntry.stopwatch.Elapsed;
            methodTimes.Add(new MethodTime(
                stackTraceEntry.methodName,
                time
            ));
            if(methodTimes.Count > writeThreshold){
                Task.Run(() => WriteBag());
            }
        }

        private static void WriteBag(){
            if(fileWriteSemaphore.CurrentCount < 1){
                //skip; another thread is already writing, so this one would have little
                //to write when semaphore becomes available
                return;
            }
            fileWriteSemaphore.Wait();
            var lines = from methodTime in new ProdComDrainEnumerable<MethodTime>(methodTimes)
                        select $"{methodTime.methodName},{methodTime.time.TotalMilliseconds}ms";
            File.AppendAllLines(logFilePath,lines);
            fileWriteSemaphore.Release();
        }

        private class StackTraceEntry{
            internal string methodName;
            internal Stopwatch stopwatch = new();
            internal string? callerName = null;
            internal TimeSpan? callTime = null;

            internal StackTraceEntry(string methodName){
                stopwatch.Start();
                this.methodName = methodName;
            }

            internal StackTraceEntry(
                string methodName,
                string callerName,
                TimeSpan callTime): this(methodName){
                this.callerName = callerName;
                this.callTime = callTime;
            }

            internal StackTraceEntry(
                string methodName,
                StackTraceEntry caller
            ): this(
                methodName,
                caller.methodName,
                caller.stopwatch.Elapsed
            ){}
        }

        private class MethodTime{
            internal string methodName;
            internal TimeSpan time;
            internal string? callerName = null;
            internal TimeSpan? callTime = null;

            internal MethodTime(string methodName, TimeSpan time){
                this.methodName = methodName;
                this.time = time;
            }

            internal MethodTime(
                string methodName,
                TimeSpan time,
                string callerName,
                TimeSpan callTime){
                this.methodName = methodName;
                this.time = time;
                this.callerName = callerName;
                this.callTime = callTime;
            }
        }

        private class ProdComDrainEnumerable<T> : IEnumerable<T> where T : class
        {
            private IProducerConsumerCollection<T> collection;

            internal ProdComDrainEnumerable(IProducerConsumerCollection<T> collection){
                this.collection = collection;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new ProdComDrainEnumerator<T>(collection);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ProdComDrainEnumerator<T>: IEnumerator<T> where T:class{
            private IProducerConsumerCollection<T> collection;
            private bool hasNext = false;
            private T? next;

            public ProdComDrainEnumerator(IProducerConsumerCollection<T> collection){
                this.collection = collection;
            }

            public void Reset(){
                throw new NotSupportedException();
            }

            public bool MoveNext(){
                hasNext = collection.TryTake(out next);
                return hasNext;
            }

            public void Dispose()
            {
                //nothing to dispose
            }

            public T Current {
                get {
                    if (hasNext) return next!;
                    else throw new NotSupportedException("no values in IEnumerator");
                }
            }

            object IEnumerator.Current => Current;

            
        }
    }
}