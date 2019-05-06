using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmbientWarClient
{
    public class VariableSpace
    {
        private Dictionary<string, dynamic> m_variables = new Dictionary<string, dynamic>();

        private List<Tuple<string, Func<string, dynamic, Task>>> m_setHandlers = new List<Tuple<string, Func<string, dynamic, Task>>>();

        private List<Tuple<string, Func<string, dynamic, Task>>> m_newSetHandlers = new List<Tuple<string, Func<string, dynamic, Task>>>();
        
        private string m_spaceId;

        public VariableSpace(string spaceId)
        {
            m_spaceId = spaceId;
        }

        public static VariableSpace Create(string spaceId)
        {
            var space = VariableSpaceDispatcher.GetVariableSpace(spaceId);

            if (space == null)
            {
                space = new VariableSpace(spaceId);
                VariableSpaceDispatcher.AddVariableSpace(space);

                BaseScript.TriggerServerEvent("ss:makeVarSpace", spaceId);
            }

            return space;
        }

        public string SpaceId
        {
            get
            {
                return m_spaceId;
            }
        }

        public dynamic this[string key]
        {
            get
            {
                dynamic val;

                if (m_variables.TryGetValue(key, out val))
                {
                    return val;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SyncValue(key, value);
            }
        }

        public async Task SetValueNoSync(string key, dynamic value)
        {
            m_variables[key] = value;

            Debug.WriteLine("set handler for " + key + " " + m_setHandlers.Count);

            foreach (var handler in m_setHandlers)
            {
                if (key.StartsWith(handler.Item1))
                {
                    try
                    {
                        await handler.Item2(key, value);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            }
        }

        private void SyncValue(string key, dynamic value)
        {
            BaseScript.TriggerServerEvent("ocw:varSpace:reqSet", m_spaceId, key, value);
        }

        public void RegisterSetHandler(string prefix, Func<string, dynamic, Task> setHandler)
        {
            Debug.WriteLine("registering set handler for " + prefix);

            m_setHandlers.Add(Tuple.Create(prefix, setHandler));
            m_newSetHandlers.Add(Tuple.Create(prefix, setHandler));
        }

        public async Task Tick()
        {
            foreach (var setHandler in m_newSetHandlers)
            {
                foreach (var variable in m_variables)
                {
                    if (variable.Key.StartsWith(setHandler.Item1))
                    {
                        try
                        {
                            await setHandler.Item2(variable.Key, variable.Value);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                        }
                    }
                }
            }

            m_newSetHandlers.Clear();
        }
    }

    public class VariableSpaceDispatcher : BaseScript
    {
        private static Dictionary<string, VariableSpace> ms_variableSpaces = new Dictionary<string, VariableSpace>();

        internal static void AddVariableSpace(VariableSpace space)
        {
            ms_variableSpaces.Add(space.SpaceId, space);
        }

        internal static VariableSpace GetVariableSpace(string spaceId)
        {
            VariableSpace space;

            if (ms_variableSpaces.TryGetValue(spaceId, out space))
            {
                return space;
            }
            else
            {
                return null;
            }
        }

        public VariableSpaceDispatcher()
        {
            EventHandlers["ocw:varSpace:create"] += new Action<string, dynamic>(async (space, dataMap) =>
            {
                VariableSpace varSpace;

                if (!ms_variableSpaces.TryGetValue(space, out varSpace))
                {
                    varSpace = new VariableSpace(space);
                    ms_variableSpaces[space] = varSpace;
                }

                var dictionary = dataMap as IDictionary<string, object>;

                Debug.WriteLine("variable space {0} created (map is {1}, dict is {2})", space, dataMap.GetType().Name, dictionary);

                if (dictionary != null)
                {
                    foreach (var kvp in dictionary)
                    {
                        Debug.WriteLine("setting {0} in space {1} to {2}", kvp.Key, space, kvp.Value);

                        await varSpace.SetValueNoSync(kvp.Key, kvp.Value);
                    }
                }
            });

            EventHandlers["ocw:varSpace:set"] += new Action<string, string, dynamic>((space, key, value) =>
            {
                // get the variable space requested
                VariableSpace varSpace;

                if (ms_variableSpaces.TryGetValue(space, out varSpace))
                {
                    Debug.WriteLine("setting {0} in space {1} to {2}", key, space, value);

                    varSpace.SetValueNoSync(key, value);
                }
            });

            EventHandlers["onClientResourceStart"] += new Action<string>(a =>
            {
                if (a == GetCurrentResourceName())
                {
                    TriggerServerEvent("ocw:varSpace:resync");
                }
            });

            Tick += VariableSpaceDispatcher_Tick;
        }

        async Task VariableSpaceDispatcher_Tick()
        {
            foreach (var space in ms_variableSpaces)
            {
                await space.Value.Tick();
            }
        }
    }
}