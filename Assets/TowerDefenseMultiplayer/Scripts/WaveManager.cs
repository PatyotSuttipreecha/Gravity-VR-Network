/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
	/// Handling all wave logic in a game map. Provides access to the current wave Index and wave events.
	/// Waves are not spawned individually but requested from the server on all clients via RPCs.
	/// </summary>
    public class WaveManager : NetworkBehaviour
    {
        //reference to this script instance
        private static WaveManager instance;

        /// <summary>
        /// Current wave index in readable format, begins at 1.
        /// </summary>
        public static int waveIndex;

        /// <summary>
		/// Mask for all Unit game objects, if necessary for e.g. raycasting against them.
		/// </summary>
        public static int layerMask;

        /// <summary>
		/// Fired before a new wave starts with the remaining delay in seconds.
		/// </summary>
        public static event Action<int> wavePreEvent;

        /// <summary>
		/// Fired when a wave starts providing the current wave index.
		/// </summary>
        public static event Action<int> waveStartEvent;

        /// <summary>
		/// Fired when a wave has finished spawning all units defined for that wave.
		/// </summary>
        public static event Action<int> waveEndEvent;

        /// <summary>
		/// Difficulty multiplier value (see below) that is set and distributed by the server.
		/// </summary>
        [HideInInspector]
        public NetworkVariable<float> difficultyFactor = new NetworkVariable<float>(1f);

        /// <summary>
        /// Local multiplier for increasing unit health depending on player count.
        /// Does not have any effect when not running as a server.
        /// </summary>
        public float difficultyMultiplier = 0;

        /// <summary>
		/// The list of waves defined in the inspector for the current map.
		/// </summary>
        public List<Wave> waves;

        /// <summary>
		/// Dictionary caching all Unit components that were ever spawned during the game in this map.
		/// </summary>
        public Dictionary<GameObject, Unit> allUnits = new Dictionary<GameObject, Unit>();

        /// <summary>
		/// Dictionary caching the active Unit components on their respective path as collected in their last frame index.
        /// We need this because we cannot access pathUnitsActive directly, since that could be modified every frame and is therefore not persistent.
		/// </summary>
		public Dictionary<SplineContainer, List<Unit>> pathUnitsLast = new Dictionary<SplineContainer, List<Unit>>();

        //splitting all existing SplineContainers in multiple indices so we can process them in batches
        private Dictionary<int, List<SplineContainer>> pathsIndexed = new Dictionary<int, List<SplineContainer>>();
        //dictionary caching the Unit components that are currently active in the scene i.e. moving on their respective path
        private static protected Dictionary<SplineContainer, List<Unit>> pathUnitsActive = new Dictionary<SplineContainer, List<Unit>>();
        //cached list for checking whether a player exists/was spawned at a specific path location
        private List<bool> playerAtPath;
        
        private int frameIndex = 0;
        private int activeCount = 0;
        private WaitForSeconds waitDelay;


        //initialize variables
        void Awake()
        {
            instance = this;
            waveIndex = 0;
            layerMask = LayerMask.GetMask("Unit");
            waitDelay = new WaitForSeconds(1);
            playerAtPath = new List<bool>(new bool[NetworkManagerCustom.GetInstance().maxPlayers]);

            for (int i = 0; i < 10; i++)
            {
                pathsIndexed.Add(i, new List<SplineContainer>());
            }

            if (!NetworkManager.Singleton.IsServer) return;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            difficultyFactor.Value = 1 + (NetworkManager.Singleton.ConnectedClientsList.Count - 1) * difficultyMultiplier;
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static WaveManager GetInstance()
        {
            return instance;
        }


        //initialize dictionary cache lists with maximum length
        void Start()
        {
            for (int i = 0; i < PathManager.GetInstance().splinePaths.Count; i++)
            {
                SplineContainer path = PathManager.GetInstance().splinePaths[i];
                pathsIndexed[i % 10].Add(path);

                int maxUnitSpawn = 0;
                for (int j = 0; j < waves.Count; j++)
                {
                    for(int k = 0; k < waves[j].entries.Count; k++)
                    {
                        WaveEntry entry = waves[j].entries[k];
                        if ((entry.path == null || entry.path == path) && entry.count > maxUnitSpawn)
                            maxUnitSpawn = entry.count;
                    }
                }

                pathUnitsActive.Add(path, new List<Unit>(maxUnitSpawn));
                pathUnitsLast.Add(path, new List<Unit>(maxUnitSpawn));
            }
        }


        /// <summary>
        /// Once the server has finished initializing the network, we can access player data that was synced before.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            foreach (PlayerStruct playerStruct in NetworkManagerCustom.GetInstance().playerData)
            {
                playerAtPath[playerStruct.thisColor] = true;
            }
        }


        //clear and collect active Unit components and add them to the cache
        //the collection process is split over several frames only selecting some paths each time
        void Update()
        {
            frameIndex = Time.frameCount % 10;

            for(int i = 0; i < pathsIndexed[frameIndex].Count; i++)
            {
                pathUnitsLast[pathsIndexed[frameIndex][i]].Clear();
                pathUnitsLast[pathsIndexed[frameIndex][i]].AddRange(pathUnitsActive[pathsIndexed[frameIndex][i]]);
            }
        }


        //this method is called when the server and all clients have finished loading the map scene (or timed out)
        void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            //as a server, start the waves spawn routine
            StartCoroutine(StartWaves());
        }


        //main routine for spawning enemy units, run on the server only
        IEnumerator StartWaves()
        {
            //additional wait for all scripts and additive scenes to completely load
            yield return new WaitForSeconds(5);

            for (int i = 0; i < waves.Count; i++)
            {
                if (waves[i].startDelay > 0)
                {
                    WavePreEventClientRpc(waves[i].startDelay);
                    yield return new WaitForSeconds(waves[i].startDelay);
                }

                StartCoroutine(SpawnWaveServer(waves[i], i));

                while (activeCount > 0)
                {
                    yield return waitDelay;
                }
            }

            StartCoroutine(CheckGameEnd());
        }


        //check for units that are still alive after spawning finished
        //the server ends the game if all units have been defeated
        IEnumerator CheckGameEnd()
        {
            while(true)
            {
                bool hasActive = false;
                foreach(List<Unit> list in pathUnitsActive.Values)
                {
                    if (list.Count > 0)
                    {
                        yield return waitDelay;
                        hasActive = true;
                        break;
                    }
                }

                if (hasActive) continue;
                else break;
            }

            GameManager.GetInstance().GameOverClientRpc(true);
        }


        //individual routine for one specific wave, run on the server
        //the server sends start and end events to clients so they will run the spawn logic too
        IEnumerator SpawnWaveServer(Wave wave, int index)
        {
            WaveStartEventClientRpc(index);
            activeCount++;

            float waveLength = 0;
            for (int i = 0; i < wave.entries.Count; i++)
            {
                float currentLength = (wave.entries[i].count - 1) * wave.entries[i].spawnDelay;
                if (currentLength > waveLength) waveLength = currentLength;

                StartCoroutine(SpawnWaveEntry(wave.entries[i]));
            }

            yield return new WaitForSeconds(waveLength);

            activeCount--;
            WaveEndEventClientRpc(index);
        }


        //only run on the client
        //spawn a specific wave locally as instructed by the server via WaveStartEventClientRpc
        void SpawnWaveClient(int index)
        {
            Wave wave = waves[index];

            for (int i = 0; i < wave.entries.Count; i++)
            {
                StartCoroutine(SpawnWaveEntry(wave.entries[i]));
            }
        }


        //the actual Unit spawning routine inside a Wave entry
        //loops over the unit count, spawns them and assigns their path
        IEnumerator SpawnWaveEntry(WaveEntry entry)
        {
            //path(s) to spawn units on. As per path variable description, on all paths if none is set
            SplineContainer[] paths = entry.path == null ? PathManager.GetInstance().splinePaths.ToArray() : new SplineContainer[] { entry.path };

            for (int i = 0; i < entry.count; i++)
            {
                for (int j = 0; j < paths.Length; j++)
                {
                    //if the path is null, check if we have to spawn units at that path
                    //depending on whether a player at that location exists
                    if (entry.path == null && !entry.withoutPlayer && !playerAtPath[j])
                        continue;

                    //spawn unit
                    GameObject obj = PoolManager.Spawn(entry.prefab, paths[j].transform.position + (Vector3)paths[j][0][0].Position, Quaternion.identity);
                    Unit unit = obj.GetComponent<Unit>();
                    unit.StartMove(paths[j]);

                    if (!allUnits.ContainsKey(obj))
                        allUnits.Add(obj, unit);
                }

                yield return new WaitForSeconds(entry.spawnDelay);
            }
        }


        /// <summary>
        /// Notifies clients that the next wave will start after the delay provided.
        /// </summary>
        [ClientRpc]
        public void WavePreEventClientRpc(int delay)
        {
            waveIndex++;
            wavePreEvent?.Invoke(delay);
        }


        /// <summary>
        /// Instructs clients to start spawning a specific wave.
        /// </summary>
        [ClientRpc]
        public void WaveStartEventClientRpc(int index)
        {
            if(!IsServer)
                SpawnWaveClient(index);

            waveStartEvent?.Invoke(index);
        }


        /// <summary>
        /// Notifies clients that spawning of a specific wave ended.
        /// </summary>
        [ClientRpc]
        public void WaveEndEventClientRpc(int index)
        {
            waveEndEvent?.Invoke(index);
        }


        /// <summary>
        /// Called from the Unit component adding themselves to the dictionary cache.
        /// </summary>
        public static void AddPathUnit(SplineContainer path, Unit unit)
        {
            pathUnitsActive[path].Add(unit);
        }


        /// <summary>
        /// Called from the Unit component removing themselves from the dictionary cache.
        /// </summary>
        public static void RemovePathUnit(SplineContainer path, Unit unit)
        {
            pathUnitsActive[path].Remove(unit);
        }


        /// <summary>
		/// Clears static content.
		/// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            pathUnitsActive.Clear();
        }
    }


    /// <summary>
    /// A wave definition that can have multiple spawn entries.
    /// </summary>
    [Serializable]
    public class Wave
    {
        /// <summary>
		/// The delay before the wave spawning starts.
		/// </summary>
        public int startDelay = 0;

        /// <summary>
		/// List of entries to be spawned in this wave.
		/// </summary>
        public List<WaveEntry> entries = new List<WaveEntry>();
    }


    /// <summary>
    /// An entry definition that describes what and where to spawn during a wave.
    /// </summary>
    [Serializable]
    public class WaveEntry
    {
        /// <summary>
		/// Unit prefab to spawn.
		/// </summary>
        public GameObject prefab = null;

        /// <summary>
		/// Amount of units to spawn.
		/// </summary>
        public int count = 1;

        /// <summary>
		/// Time to wait between units.
		/// </summary>
        public float spawnDelay = 1;

        /// <summary>
		/// Path the units should get assigned to.
        /// If no path is set, units spawn on all paths.
		/// </summary>
        public SplineContainer path = null;

        /// <summary>
		/// Whether the units are spawned even without a player at that location.
        /// Only used when the path was not set, so if false, does not blindly spawn on all paths. 
		/// </summary>
        public bool withoutPlayer = false;
    }


    /// <summary>
    /// Class to cache all components of a Unit we are accessing very often during the game. 
    /// </summary>
    public class UnitCache
    {
        /// <summary>
		/// Its GameObject for spawning or getting components.
		/// </summary>
        public GameObject obj;

        /// <summary>
		/// Its Transform for position and distance calculations.
		/// </summary>
        public Transform trans;

        /// <summary>
		/// The actual Unit component.
		/// </summary>
        public Unit unit;


        /// <summary>
		/// Constructor assigning all of the variables above.
		/// </summary>
        public UnitCache(GameObject gameObj)
        {
            obj = gameObj;
            trans = obj.transform;
            unit = obj.GetComponent<Unit>();
        }
    }
}