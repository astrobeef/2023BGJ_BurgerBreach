using System;
using System.Threading;

namespace Model
{
    public static class ActionPoster
    {
        public static bool PostAction(Action action)
        {
            if (action != null)
            {
                main.Instance.Context.Post(_ => action(), null);
                return true;
            }
            
            return false;
        }
        public static bool PostAction<T1>(Action<T1> action, T1 param1)
        {
            if (action != null)
            {
                main.Instance.Context.Post(_ => action(param1), null);
                return true;
            }
            
            return false;
        }
        public static bool PostAction<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            if (action != null)
            {
                main.Instance.Context.Post(_ => action(param1, param2), null);
                return true;
            }

            return false;
        }
        public static bool PostAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            if (action != null)
            {
                main.Instance.Context.Post(_ => action(param1, param2, param3), null);
                return true;
            }

            return false;
        }
        public static bool PostAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (action != null)
            {
                main.Instance.Context.Post(_ => action(param1, param2, param3, param4), null);
                return true;
            }
            
            return false;
        }

        

        public static bool AwaitAction(ref bool trigger, Func<bool> action)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action();
        }
        public static bool AwaitAction<T1>(ref bool trigger, Func<T1, bool> action, T1 param1)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1);
        }
        public static bool AwaitAction<T1, T2>(ref bool trigger, Func<T1, T2, bool> action, T1 param1, T2 param2)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1, param2);
        }
        public static bool AwaitAction<T1, T2, T3>(ref bool trigger, Func<T1, T2, T3, bool> action, T1 param1, T2 param2, T3 param3)
        {
            while (!trigger)
            {
                Thread.Sleep(1);
            }
            trigger = false;
            return action(param1, param2, param3);
        }
    }
}