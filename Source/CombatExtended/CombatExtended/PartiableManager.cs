using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class PartiableManager
    {
        protected struct PartiableUpdateRequest
        {
            public int minX;
            public int minZ;
            public int maxX;
            public int maxZ;
            public float value;

            public bool IsValid
            {
                get => maxX > minX && maxZ > minZ;
            }

            public void Clamp(int sizeX, int sizeZ)
            {
                minX = Mathf.Clamp(minX, 0, sizeX - 1);
                maxX = Mathf.Clamp(maxX, 0, sizeX - 1);
                minZ = Mathf.Clamp(minZ, 0, sizeZ - 1);
                maxZ = Mathf.Clamp(maxZ, 0, sizeZ - 1);
            }

            public static PartiableUpdateRequest Create(IntVec3 cell, float value, int radius)
            {
                PartiableUpdateRequest zone = new PartiableUpdateRequest();
                zone.minX = cell.x - radius;
                zone.minZ = cell.z - radius;
                zone.maxX = cell.x + radius;
                zone.maxZ = cell.z + radius;
                zone.value = value;
                return zone;
            }
        }

        public Map map;
        public PartiableGrid grid;
        
        private int sizeX;
        private int sizeZ;
        private CellIndices cellIndices;
        private ThreadStart threadStart;
        private Thread thread;
        private object locker = new object();
        private int oldestQueuedAt = -1;
        private int offThreadCount;
        private readonly List<PartiableUpdateRequest> mainThreadQueue = new List<PartiableUpdateRequest>();
        private readonly List<PartiableUpdateRequest> offThreadQueue = new List<PartiableUpdateRequest>();

        private bool mapIsAlive = true;

        public PartiableManager(Map map, int unitTicks = 60)
        {
            this.map = map;           
            grid = new PartiableGrid(map, unitTicks);
            sizeX = map.cellIndices.mapSizeX;
            sizeZ = map.cellIndices.mapSizeZ;
            cellIndices = map.cellIndices;
            threadStart = new ThreadStart(OffMainThreadLoop);
            thread = new Thread(threadStart);
            thread.Start();            
        }

        public virtual void Tick()
        {
            if (mainThreadQueue.Count == 0)
                return;
            offThreadCount = 0;
            lock (locker)
            {
                offThreadQueue.AddRange(mainThreadQueue);
                offThreadCount = offThreadQueue.Count;
            }
            mainThreadQueue.Clear();
            oldestQueuedAt = -1;
        }        

        public virtual void Notify_MapRemoved()
        {
            try
            {
                mapIsAlive = false;
                thread.Join();
            }
            catch (Exception er)
            {
                Log.Error($"CE: SightGridManager Notify_MapRemoved failed to stop thread with {er}");
            }
        }

        public void Set(IntVec3 cell, float value, int radius)
        {
            if (cell.InBounds(map))
                Enqueue(cell, value, radius);
        }

        private void Apply(PartiableUpdateRequest request)
        {
            //
            // apply the new values...            
            for (int i = request.minX; i <= request.maxX; i++)
                for (int j = request.minZ; j <= request.maxZ; j++)
                    grid[cellIndices.CellToIndex(i, j)] += request.value;
        }

        private void Enqueue(IntVec3 cell, float value, int radius)
        {
            if (mainThreadQueue.Count == 0)
                oldestQueuedAt = GenTicks.TicksGame;

            PartiableUpdateRequest request;
            request = PartiableUpdateRequest.Create(cell, value, radius);
            request.Clamp(map.cellIndices.mapSizeX, map.cellIndices.mapSizeZ);
            if(request.IsValid)
                mainThreadQueue.Add(request);
        }

        private void OffMainThreadLoop()
        {
            PartiableUpdateRequest request = default(PartiableUpdateRequest);
            int dangerRectLeft = 0;
            bool dequeued = false;
            while (mapIsAlive)
            {
                dangerRectLeft = 0;
                dequeued = false;
                lock (locker)
                {
                    if ((dangerRectLeft = offThreadQueue.Count) > 0)
                    {
                        dequeued = true;
                        request = offThreadQueue[0];                        
                        offThreadQueue.RemoveAt(0);
                    }
                }
                // threading goes brrrrrr
                if (dequeued)
                    Apply(request);
                // sleep so other threads can do stuff
                if (dangerRectLeft == 0)
                    Thread.Sleep(1);
            }
            Log.Message("CE: AvoidanceTracker thread stopped!");
        }
    }
}

