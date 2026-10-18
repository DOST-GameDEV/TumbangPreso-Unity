using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TumbangPreso.UI
{
    // Compilation only: no reflected method is invoked and no view is constructed.
    public static class UiCodePreparation
    {
        private static readonly Type[] Types={typeof(Hub.HubHero),typeof(Hub.HubKit),typeof(Hub.HubScenery),
            typeof(Hub.HubButton),typeof(Hub.HubShape),typeof(Hub.HubStyle),typeof(ModelPreview),
            typeof(TumpAbilitySymbol),typeof(CrispUiText),typeof(OwnerCharacterStories)};
        public static int PreparedMethods {get;private set;}
        public static bool Complete {get;private set;}
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){PreparedMethods=0;Complete=false;}
        public static IEnumerator Prepare()
        {
            if(Complete)yield break;
#if ENABLE_MONO
            var types=new Queue<Type>(Types);
            long began=System.Diagnostics.Stopwatch.GetTimestamp();
            while(types.Count>0)
            {
                var type=types.Dequeue();
                foreach(var nested in type.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))
                    if(!nested.ContainsGenericParameters)types.Enqueue(nested);
                var methods=new List<MethodBase>();methods.AddRange(type.GetMethods(Flags));methods.AddRange(type.GetConstructors(Flags));
                foreach(var method in methods)
                {
                    if(method.ContainsGenericParameters||method.IsAbstract||method.GetMethodBody()==null)continue;
                    // Mono PrepareMethod is a no-op. Requesting the compiled entry
                    // point prepares this method without executing its behavior.
                    var entry=method.MethodHandle.GetFunctionPointer();
                    if(entry==IntPtr.Zero)throw new InvalidOperationException("UI method preparation returned no compiled entry.");
                    PreparedMethods++;
                    if((System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000.0/System.Diagnostics.Stopwatch.Frequency>=2)
                    {yield return null;began=System.Diagnostics.Stopwatch.GetTimestamp();}
                }
                yield return null;began=System.Diagnostics.Stopwatch.GetTimestamp();
            }
#endif
            Complete=true;
        }
    }
}
