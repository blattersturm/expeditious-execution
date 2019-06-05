using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BaseRush
{
    public class VariableManager : BaseScript
    {
        private Dictionary<string, object> m_variables = new Dictionary<string, object>();
        private HashSet<string> m_hostVariables = new HashSet<string>();

        private string m_lastSource;

        private bool m_debug = false;//IsDuplicityVersion();

        public static VariableAPI API { get; private set; }

        public VariableManager()
        {
            API = new VariableAPI(this);
        }

        [EventHandler("onMapStart")]
        public void OnMapStart()
        {
            m_variables.Clear();
        }

        [EventHandler("onClientMapStart")]
        public void OnClientMapStart()
        {
            m_variables.Clear();
        }

#if CLIENT
        [EventHandler("onClientGameTypeStart")]
        public void OnClientGameTypeStart()
        {
            TriggerServerEvent("br:sendMyVars");
        }
#else
        [Tick]
        public async System.Threading.Tasks.Task OnTick()
        {
            if (Players.Count() == 0)
            {
                m_variables.Clear();
            }
        }
#endif

        private void EmitTarget(string ev, params object[] args)
        {
            #if SERVER
            foreach (var player in Players)
            {
                if (player.Handle != m_lastSource.Substring(4))
                {
                    player.TriggerEvent(ev, args);
                }
            }
            #else
            TriggerServerEvent(ev, args);
            #endif
        }
        
        private bool SecureHost(string key)
        {
            return false;
        }

        private string FormatSource()
        {
            #if SERVER
            if (m_lastSource == null)
            {
                return "** Invalid **";
            }

            return GetPlayerName(m_lastSource.Substring(4)) + "^7";
            #else
            return "Server";
            #endif
        }

        [EventHandler("br:variableSet")]
        public void VariableSet([FromSource] string source, string key, object value, bool local)
        {
            m_lastSource = source;

            if (SecureHost(key))
            {
                return;
            }

            m_variables[key] = value;

            if (m_debug)
            {
                Debug.WriteLine($"{FormatSource()} set {key} to {value}");
            }

            if (local || IsDuplicityVersion())
            {
                EmitTarget("br:variableSet", key, value, false);
            }
        }

        [EventHandler("br:variableHSet")]
        public void VariableHSet([FromSource] string source, string key, string h, object value, bool local)
        {
            m_lastSource = source;

            if (SecureHost(key))
            {
                return;
            }

            if (!m_variables.ContainsKey(key))
            {
                m_variables[key] = new Dictionary<string, object>();
            }

            if (m_variables[key] is IDictionary<string, object> hash)
            {
                hash[h] = value;
            }

            if (m_debug)
            {
                Debug.WriteLine($"{FormatSource()} hset {key}[{h}] to {value}");
            }

            if (local || IsDuplicityVersion())
            {
                EmitTarget("br:variableHSet", key, h, value, false);
            }
        }

        [EventHandler("br:variableLAdd")]
        public void VariableLAdd([FromSource] string source, string key, object value, bool local)
        {
            m_lastSource = source;

            if (SecureHost(key))
            {
                return;
            }

            if (!m_variables.ContainsKey(key))
            {
                m_variables[key] = new List<object>();
            }

            if (m_variables[key] is IList<object> list)
            {
                list.Add(value);
            }

            if (m_debug)
            {
                Debug.WriteLine($"{FormatSource()} ladd {key}, {value}");
            }

            if (local || IsDuplicityVersion())
            {
                EmitTarget("br:variableLAdd", key, value, false);
            }
        }

        [EventHandler("br:variableLRem")]
        public void VariableLRem([FromSource] string source, string key, int i, bool local)
        {
            m_lastSource = source;

            if (SecureHost(key))
            {
                return;
            }

            if (!m_variables.ContainsKey(key))
            {
                return;
            }

            if (m_variables[key] is IList<object> list)
            {
                list.RemoveAt(i);
            }

            if (m_debug)
            {
                Debug.WriteLine($"{FormatSource()} lrem {key}, {i}");
            }

            if (local || IsDuplicityVersion())
            {
                EmitTarget("br:variableLRem", key, i, false);
            }
        }

        #if SERVER
        [EventHandler("br:sendMyVars")]
        public void SendMyVars([FromSource] Player source)
        {
            foreach (var entry in m_variables)
            {
                source.TriggerEvent("br:variableSet", entry.Key, entry.Value);
            }
        }
        #endif

        public class VariableAPI
        {
            private VariableManager m_varman;

            public VariableAPI(VariableManager varman)
            {
                m_varman = varman;
            }

            public void set(string key, object value)
            {
                m_varman.VariableSet(null, key, value, true);
            }

            public object get(string key)
            {
                if (!m_varman.m_variables.ContainsKey(key))
                {
                    return null;
                }

                return m_varman.m_variables[key];
            }

            public void hset(string key, string h, object value)
            {
                m_varman.VariableHSet(null, key, h, value, true);
            }

            public object hget(string key, string h)
            {
                if (m_varman.m_variables.ContainsKey(key))
                {
                    if (m_varman.m_variables[key] is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue(h, out var val))
                        {
                            return val;
                        }
                    }
                }

                return null;
            }

            public void ladd(string key, object value)
            {
                m_varman.VariableLAdd(null, key, value, true);
            }

            public void lrem(string key, int i)
            {
                m_varman.VariableLRem(null, key, i, true);
            }

            public object lget(string key, int i)
            {
                if (m_varman.m_variables.ContainsKey(key))
                {
                    if (m_varman.m_variables[key] is IList<object> list)
                    {
                        if (i < list.Count)
                        {
                            return list[i];
                        }
                    }
                }

                return null;
            }

            public int lcount(string key)
            {
                if (m_varman.m_variables.ContainsKey(key))
                {
                    if (m_varman.m_variables[key] is IList<object> list)
                    {
                        return list.Count;
                    }
                }

                return 0;
            }
        }

#if EXPORT_PROXY
        private CallbackDelegate m_lget;
        private CallbackDelegate m_get;
        private CallbackDelegate m_ladd;
        private CallbackDelegate m_lcount;
        private CallbackDelegate m_set;
        private CallbackDelegate m_hget;
        private CallbackDelegate m_hset;

        public VariableManager(dynamic vm)
        {
            /*var dict = (IDictionary<string, object>)vm;
            m_lget = (CallbackDelegate)dict["lget"];
            m_get = (CallbackDelegate)dict["get"];
            m_set = (CallbackDelegate)dict["set"];
            m_ladd = (CallbackDelegate)dict["ladd"];
            m_lcount = (CallbackDelegate)dict["lcount"];
            m_hget = (CallbackDelegate)dict["hget"];
            m_hset = (CallbackDelegate)dict["hset"];*/
            m_get = GetMember("get");
            m_set = GetMember("set");
            m_lget = GetMember("lget");
            m_hget = GetMember("hget");
            m_hset = GetMember("hset");
            m_lcount = GetMember("lcount");
            m_ladd = GetMember("ladd");
        }

        private class DelegateFn
		{
			public CallbackDelegate Delegate { get; set; }
		}

		public CallbackDelegate GetMember(string name)
		{
			// get the event name
			var eventName = $"__cfx_export_base-rush_{name}";

			// get the member
			var exportDelegate = new DelegateFn();
			BaseScript.TriggerEvent(eventName, new Action<CallbackDelegate>(a => exportDelegate.Delegate = a));

            return exportDelegate.Delegate;
        }

        public object lget(string key, int idx)
        {
            return m_lget.Invoke(key, idx);
        }

        public void ladd(string key, object value)
        {
            m_ladd.Invoke(key, value);
        }

        public int lcount(string key)
        {
            return (int)m_lcount.Invoke(key);
        }

        public object get(string key)
        {
            return m_get.Invoke(key);
        }

        public void set(string key, object value)
        {
            m_set.Invoke(key, value);
        }

        public void hset(string key, string idx, object value)
        {
            m_hset.Invoke(key, idx, value);
        }

        public object hget(string key, string idx)
        {
            return m_hget.Invoke(key, idx);
        }
#endif
    }
}