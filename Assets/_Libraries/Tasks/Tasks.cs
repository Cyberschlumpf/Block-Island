using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

public class Tasks {

    /*private static Tasks _instance;
    public static Tasks instance {
        get {
            if (_instance == null) _instance = GameObject.FindObjectOfType<Tasks>();
            return _instance;
        }
    }*/

    //private readonly List<Action> unityThreadTasks = new List<Action>();


    /*void OnDestroy() {
        lock (unityThreadTasks) {
            unityThreadTasks.Clear();
        }
    }

    void Update() {
        lock (unityThreadTasks) {
            foreach (Action act in unityThreadTasks) {
                act();
            }
            unityThreadTasks.Clear();
        }
    }

    public void InvokeInUnityThread(params Action[] actions) {
        lock (unityThreadTasks) {
            unityThreadTasks.AddRange(actions);
        }
    }*/

    public static void InvokeSync(params Action[] actions) {
		ManualResetEvent[] events = new ManualResetEvent[actions.Length];
		for(int i=0; i<events.Length; i++) {
			events[i] = new ManualResetEvent(false);
		}

		for(int i=0; i<actions.Length; i++) {
			Action action = actions[i];
			ManualResetEvent evt = events[i];
			ThreadPool.QueueUserWorkItem( arg => {
				action();
				evt.Set();
			} );
		}

		WaitHandle.WaitAll( events );
	}

    public static void InvokeASync(params Action[] actions) {
        for (int i = 0; i < actions.Length; i++) {
            Action action = actions[i];
            ThreadPool.QueueUserWorkItem(arg => {
                action();
            });
        }
    }


    public static void ParallelFor(Action<int, int> loop, int count) {
        int subCount = count / SystemInfo.processorCount;

        ManualResetEvent[] events = new ManualResetEvent[SystemInfo.processorCount];
        for (int i = 0; i < SystemInfo.processorCount; i++) {
            int from = subCount * i;
            if (i == SystemInfo.processorCount - 1) {
                events[i] = InvokeLoop(loop, from, count);
            } else {
                events[i] = InvokeLoop(loop, from, from + subCount);
            }
        }
        WaitHandle.WaitAll(events);
    }
    private static ManualResetEvent InvokeLoop(Action<int, int> loop, int from, int to) {
        ManualResetEvent evt = new ManualResetEvent(false);
        ThreadPool.QueueUserWorkItem(arg => {
            loop(from, to);
            evt.Set();
        });
        return evt;
    }

}
