
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.XR;
using static Agent;

[Serializable]
public class Agent
{
    public enum AgentDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    public void Init()
    {
        positionX = depth;
        positionY = depth;
        direction = AgentDirection.None;
        currentDepth = 0;
    }

    public int positionX;
    public int positionY;

    [Range(0, 10)] public int depth;
    public int currentDepth;
    public AgentDirection direction;
    [Range(0f,1f)] public float KeepDirection;
    [Range(0, 10)] public int minDepthForBranch;
    [Range(0f,1f)] public float BranchChance;
}

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator instance;
    [SerializeField] Room[] m_availableRooms;

    [SerializeField] Agent m_agent;

    [SerializeField] public bool[,] path;

    [Header("debug")]
    public bool ActivateDebugTiles;
    [SerializeField] GameObject DebugTile_Fill;
    [SerializeField] GameObject DebugTile_Empty;
    List<GameObject> DebugTiles = new List<GameObject>();
    Vector3 DebugOffset = Vector3.zero;
    [SerializeField] Dictionary<Vector2Int,bool> Tiles = new Dictionary<Vector2Int,bool>();
    public void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);

        instance = this;
    }

    private void Start()
    {
        //ON INITIALISER L'AGENT
        m_agent.Init();

        if (ActivateDebugTiles)
        {
            DebugOffset = transform.position - new Vector3(m_agent.positionX, m_agent.positionY, 0);
            SpawnBackgroundDebugTiles(m_agent);
        }

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //ON WIPE LES TILES DE DEBUGS SI IL Y EN A
            DestroyDebugTiles();

            //ON INIT L'AGENT
            m_agent.Init();
            Tiles.Clear();
            path = GeneratePath(m_agent);
        }

    }

    public bool[,] GeneratePath(Agent agent)
    {
        //INITIALISATION DU TABLEAU
        bool[,] returnedPath = new bool[(agent.depth * 2)+1 , (agent.depth * 2) + 1];

        //ON MET TOUTES LES VALEURS A FALSE
        for (int i  = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                Tiles.Add(new Vector2Int(i, j), false);
            }
        }

        //LA TILE DE DEPART ET MISE A TRUE
        Tiles[new Vector2Int(agent.positionX, agent.positionY)] = true;

        if (ActivateDebugTiles)
            spawnDebugTile(agent.positionX, agent.positionY);


        //GENERATION
        for (int i = 0; i < agent.depth; i++)
        {
            //ON OBTIENT UNE DIRECTION VALIDE
            agent.direction = GetRandomDirection(agent.direction);

            while (agent.direction == AgentDirection.None)
            {
                agent.direction = GetRandomDirection(agent.direction);
            }

            switch (agent.direction)
            {
                case AgentDirection.Left: agent.positionX -= 1; break;
                case AgentDirection.Right: agent.positionX += 1; break;
                case AgentDirection.Up: agent.positionY += 1; break;
                case AgentDirection.Down: agent.positionY -= 1; break;
            }

            //ON AUGMENTE LA DEPTH A LAQUELLE SE TROUVE L'AGENT
            agent.currentDepth++;

            //ON MET LA VALEUR CORRESPONDANTE DU TALBEAU A TRUE
            Tiles[new Vector2Int(agent.positionX, agent.positionY)] = true;

            //ON FAIT SPAWNER UNE TILE SI LE MODE DEBUG EST ACTIVE
            if (ActivateDebugTiles)
                spawnDebugTile(agent.positionX, agent.positionY);

            if(agent.minDepthForBranch <= agent.currentDepth)
            {
                float randForBranch = UnityEngine.Random.Range(0f, 1f);
                if (randForBranch <= agent.BranchChance)
                    Branch();
            }
        }

        return returnedPath;


        AgentDirection GetRandomDirection(AgentDirection currentDirection)
        {
            //ON VERIFIE SI L'AGENT DOIT CONSERVER LA MEME DIRECTION
            float keepdir = UnityEngine.Random.Range(0f, 1f);
            if (keepdir <= agent.KeepDirection && agent.direction != AgentDirection.None)
                return currentDirection;


            bool movePosible = false;
            List<AgentDirection> possibleMoves = new List<AgentDirection>();

            if (Tiles[new Vector2Int(agent.positionX + 1, agent.positionY)] == false)
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Right);
            }

            if (Tiles[new Vector2Int(agent.positionX - 1, agent.positionY)] == false)
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Left);
            }

            if (Tiles[new Vector2Int(agent.positionX, agent.positionY +1)] == false)
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Up);
            }

            if (Tiles[new Vector2Int(agent.positionX, agent.positionY -1)] == false)
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Down);
            }
                

            if (!movePosible)
            {
                Branch();
                return agent.direction;
            }

            //ON GENERE UNE DIRECTION ALEATOIRE
            int randomDirection = UnityEngine.Random.Range(0, possibleMoves.Count);
            
            return possibleMoves[randomDirection];

           
        }

        void Branch()
        {
            //BRANCH
            
            List<Vector2Int> activeTiles = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, bool> entry in Tiles)
            {
                if (entry.Value == true)
                    activeTiles.Add(entry.Key);
            }

            int randTile = UnityEngine.Random.Range(0, activeTiles.Count);
            agent.positionX = activeTiles[randTile].x;
            agent.positionY = activeTiles[randTile].y;
            agent.direction = AgentDirection.None;
            Debug.Log("branch triggered at depth "+ agent.currentDepth+" with root at " + randTile);
        }

        void spawnDebugTile(int posX, int posY)
        {
            GameObject Tile = Instantiate(DebugTile_Fill, new Vector3(posX, posY, 0) + DebugOffset, Quaternion.identity);
            Tile.GetComponentInChildren<TextMeshProUGUI>().text = agent.currentDepth.ToString();
            DebugTiles.Add(Tile);
        }
    }
    private void DestroyDebugTiles()
    {
        foreach (var tile in DebugTiles)
            Destroy(tile);
    }

    private void SpawnBackgroundDebugTiles(Agent agent)
    {
        if (!ActivateDebugTiles)
            return;

        bool[,] returnedPath = new bool[(agent.depth * 2) + 1, (agent.depth * 2) + 1];

        for (int i = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                Instantiate(DebugTile_Empty, new Vector3(i, j, 0) + DebugOffset, Quaternion.identity);
            }
        }
    }
}
