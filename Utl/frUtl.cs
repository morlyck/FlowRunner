using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FlowRunner.Utl
{
    internal static class frUtl
    {
        public static void InvokeActionAll<typ>(this object target, object arg) {
            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (info.GetCustomAttributes(typeof(typ), true).Length == 0) continue;
                info.Invoke(target, new object[] { arg });
            }
        }
        public static void InvokeActionAll<typ>(this object target) {
            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (info.GetCustomAttributes(typeof(typ), true).Length == 0) continue;
                info.Invoke(target, null);
            }
        }
        public static void InvokeActionAll_Order<typ>(this object target, Func<typ, int> func)
            where typ : Attribute {
            Dictionary<int, Action> actions = new Dictionary<int, Action>();

            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (info.GetCustomAttributes(typeof(typ), true).Length == 0) continue;
                typ attribute = info.GetCustomAttribute<typ>();
                Action action = () => info.Invoke(target, null);
                int order = func(attribute);

                if (!actions.ContainsKey(order)) {
                    actions.Add(order, action);
                } else {
                    actions[order] += action;
                }
            }
            var actionsOrder = actions.OrderBy((x) => x.Key);
            foreach (KeyValuePair<int, Action> keyValuePair in actionsOrder) {
                keyValuePair.Value();
            }
        }

        



    }

}
