using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeLapser
{
    [HarmonyPatch(typeof(Game), "Update", new Type[] { })]
    class SafeDelayedAction
    {
        public delegate void PerformSafeAction();

        private static Queue<PerformSafeAction> actionQueue = new Queue<PerformSafeAction>();

        public static void EnqueueAction(PerformSafeAction action, int millisecondDelay = 0)
        {
            System.Threading.Timer timer = null;
            var callback = new System.Threading.TimerCallback(obj =>
            {
                lock (actionQueue)
                {
                    actionQueue.Enqueue(action);
                    timer.Dispose();
                }
            });
            timer = new System.Threading.Timer(callback,
                null, millisecondDelay, System.Threading.Timeout.Infinite);
        }

        private static void Prefix(Game __instance)
        {
            /* It should be safe to dequeue items mostly without a lock
             * since this dequeueing should only ever be happening on one thread,
             * in this one method
             */
            if(actionQueue.Count > 0)
            {
                PerformSafeAction nextAction;
                while (true)
                {
                    lock (actionQueue)
                    {
                        if(actionQueue.Count == 0)
                        {
                            break;
                        }
                        nextAction = actionQueue.Dequeue();
                    }
                    nextAction();
                } 
            }
        }
    }
}
