using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld.Planet;
using Verse;

namespace CombatExtended
{
    internal static class ICacheUtility
    {
        private static class ICache_ThingComp<T> where T : ThingComp
        {
            public static Dictionary<int, T> compsById = new Dictionary<int, T>();            
            
            public static T GetComp(Thing thing)
            { 
                if (compsById.TryGetValue(thing.thingIDNumber, out T val))
                {
                    return val;
                }
                if (thing.def.comps == null)
                {
                    return null;
                }
                if(thing.def.comps.Any(p => p.compClass == typeof(T)))
                {
                    val = thing.TryGetComp<T>();
                    if (val != null)
                    {
                        compsById[thing.thingIDNumber] = val;
                    }
                }
                return val;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Add(int id, T comp)
            {
                compsById[id] = comp;                
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Remove(int id)
            {
                if (compsById.ContainsKey(id))
                {
                    compsById.Remove(id);
                }
            }
        }

        public static T TryGetCompFast<T>(this Thing thing) where T : ThingComp
        {            
            return ICache_ThingComp<T>.GetComp(thing);
        }

        private static class ICache_MapComponent<T> where T : MapComponent
        {
            public static T[]  comps = new T[32];
            public static Map[] maps = new Map[32];
            
            public static T GetComp(Map map)
            {                
                for (int i = 0;i < maps.Length; i++)
                {
                    if (maps[i] == map)
                    {
                        return comps[i] ?? (comps[i] = map.GetComponent<T>());
                    }
                }
                return TryAdd(map, map.GetComponent<T>());
            }

            public static T TryAdd(Map map, T comp)
            {
                for (int i = 0; i < maps.Length; i++)
                {
                    if (maps[i] == null)
                    {
                        maps[i] = map;
                        return comps[i] = (comp ?? map.GetComponent<T>());
                    }
                }
                int index = maps.Length;
                Expand(ref maps, maps.Length * 2);
                Expand(ref comps, comps.Length * 2);
                maps[index] = map;
                return comps[index] = map.GetComponent<T>();
            }

            public static void Remove(Map map)
            {
                int index = -1;               
                for (int i = 0;i < maps.Length; i++)
                {
                    if(maps[i] == map)
                    {
                        index = i;
                        maps[i] = null;
                        comps[i] = null;                        
                        break;
                    }
                }
                if (index != -1 && index + 1 != maps.Length)
                {
                    for(int i = index + 1; i < maps.Length; i++)
                    {
                        if (maps[i] != null)
                        {
                            maps[i - 1] = maps[i];
                            maps[i] = null;
                            comps[i - 1] = comps[i];
                            comps[i] = null;
                            continue;
                        }
                        break;                        
                    }
                }
            }            
        }        

        public static T TryGetCompFast<T>(this Map map) where T : MapComponent
        {                        
            return ICache_MapComponent<T>.GetComp(map);
        }

        private static class ICache_GameComponent<T> where T : GameComponent
        {
            public static Game game;
            public static T component;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T TryGet(Game curGame)
            {
                if(game != curGame)
                {
                    game = curGame;
                    component = curGame.GetComponent<T>();
                }
                return component;
            }                  

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Unset()
            {
                game = null;
                component = null;
            }
        }

        public static T TryGetCompFast<T>(this Game game) where T : GameComponent
        {            
            return ICache_GameComponent<T>.TryGet(game);
        }

        private static class ICache_WorldComponent<T> where T : WorldComponent
        {
            public static World world;
            public static T component;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T TryGet(World curWorld)
            {
                if (world != curWorld)
                {
                    world = curWorld;
                    component = curWorld.GetComponent<T>();
                }
                return component;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Unset()
            {
                world = null;
                component = null;
            }
        }

        public static T TryGetCompFast<T>(this World world) where T : WorldComponent
        {
            return ICache_WorldComponent<T>.TryGet(world);
        }

        public static void Register(ThingWithComps thing)
        {
            if(thing.comps.NullOrEmpty())
            {
                return;
            }
            for(int i = 0; i < thing.comps.Count; i++)
            {
                ThingComp comp = thing.comps[i];
                if (comp != null)
                {
                    typeof(ICache_ThingComp<>).MakeGenericType(comp.GetType()).GetMethod("Add").Invoke(null, new object[] { thing.thingIDNumber, comp });
                }
            }
        }

        public static void Register(Map map)
        {
            if (map.components.NullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < map.components.Count; i++)
            {
                MapComponent component = map.components[i];
                if (component != null)
                {
                    typeof(ICache_MapComponent<>).MakeGenericType(component.GetType()).GetMethod("TryAdd").Invoke(null, new object[] { map, component });
                }
            }
        }       

        public static void DeRegister(ThingWithComps thing)
        {
            if (thing.comps.NullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < thing.comps.Count; i++)
            {
                ThingComp comp = thing.comps[i];
                if (comp != null)
                {
                    typeof(ICache_ThingComp<>).MakeGenericType(comp.GetType()).GetMethod("Remove").Invoke(null, new object[] { thing.thingIDNumber });
                }
            }
        }

        public static void DeRegister(Map map)
        {
            if (map.components.NullOrEmpty())
            {
                return;
            }            
            for (int i = 0; i < map.components.Count; i++)
            {
                MapComponent component = map.components[i];
                if (component != null)
                {
                    typeof(ICache_MapComponent<>).MakeGenericType(component.GetType()).GetMethod("Remove").Invoke(null, new object[] { map });
                }
            }
        }

        public static void DeRegister(Game game)
        {
            if (game.components.NullOrEmpty())
            {
                return;
            }            
            for (int i = 0; i < game.components.Count; i++)
            {
                GameComponent component = game.components[i];
                if (component != null)
                {
                    typeof(ICache_GameComponent<>).MakeGenericType(component.GetType()).GetMethod("Unset").Invoke(null, new object[] { });
                }
            }
        }

        public static void DeRegister(World world)
        {
            if (world.components.NullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < world.components.Count; i++)
            {
                WorldComponent component = world.components[i];
                if (component != null)
                {
                    typeof(ICache_WorldComponent<>).MakeGenericType(component.GetType()).GetMethod("Unset").Invoke(null, new object[] {});
                }
            }
        }        

        private static void Expand<T>(ref T[] array, int targetSize)
        {
            T[] temp = new T[targetSize];
            Array.Copy(array, 0, temp, 0, array.Length);
            array = temp;
        }
    }
}

