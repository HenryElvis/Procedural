
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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

    public int depth = 5;
    public int currentDepth;
    public AgentDirection direction;
    [Range(0f,1f)] public float KeepDirection;
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

            path = GeneratePath(m_agent);
        }

    }

    public bool[,] GeneratePath(Agent agent)
    {
        //INITIALISATION DU TABLEAU
        bool[,] returnedPath = new bool[(agent.depth * 2)+1 , (agent.depth * 2) + 1];

        //ON MET TOUTES LES VALEURS A FALSE
        for(int i  = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                returnedPath[i, j] = false;
            }
        }

        //LA TILE DE DEPART ET MISE A TRUE
        returnedPath[agent.positionX, agent.positionY] = true;

        if (ActivateDebugTiles)
            spawnDebugTile(agent.positionX, agent.positionY);


        //GENERATION
        for (int i = 0; i < agent.depth; i++)
        {
            //ON OBTIENT UNE DIRECTION VALIDE
            agent.direction = GetRandomDirection(agent.direction);

            switch(agent.direction)
            {
                case AgentDirection.Left: agent.positionX -= 1; break;
                case AgentDirection.Right: agent.positionX += 1; break;
                case AgentDirection.Up: agent.positionY += 1; break;
                case AgentDirection.Down: agent.positionY -= 1; break;
            }

            //ON AUGMENTE LA DEPTH A LAQUELLE SE TROUVE L'AGENT
            agent.currentDepth++;

            //ON MET LA VALEUR CORRESPONDANTE DU TALBEAU A TRUE
            returnedPath[agent.positionX, agent.positionY] = true;
            
            //ON FAIT SPAWNER UNE TILE SI LE MODE DEBUG EST ACTIVE
            if (ActivateDebugTiles)
                spawnDebugTile(agent.positionX, agent.positionY);

            
        }

        return returnedPath;


        AgentDirection GetRandomDirection(AgentDirection currentDirection)
        {
            //ON VERIFIE SI L'AGENT DOIT CONSERVER LA MEME DIRECTION
            float keepdir = UnityEngine.Random.Range(0f, 1f);
            if (keepdir <= agent.KeepDirection && agent.direction != AgentDirection.None)
                return currentDirection;

            AgentDirection returnDirection = AgentDirection.None;

            //ON GENERE UNE DIRECTION ALEATOIRE
            int randomDirection = UnityEngine.Random.Range(1, 5);

            //ON RECOMMENCE LA RECHERCHE ALEATOIRE TANT QUE LA DIRECTION PROJETE N'EST PAS VALIDE
            int projectedPosX = 0;
            int projectedPosY = 0;
            while (returnedPath[agent.positionX + projectedPosX, agent.positionY + projectedPosY] == true)
            {
                randomDirection = UnityEngine.Random.Range(1, 5);
                switch (randomDirection)
                {
                    case 1: projectedPosY = 1; break;
                    case 2: projectedPosY = -1; break;
                    case 3: projectedPosX = -1; break;
                    case 4: projectedPosX = 1; break;
                }
            }

            //ON OBTIENT LA DIRECTION CORRESPONDANTE
            switch (randomDirection)
            {
                case 1: returnDirection = AgentDirection.Up; break;
                case 2: returnDirection = AgentDirection.Down; break;
                case 3: returnDirection = AgentDirection.Left; break;
                case 4: returnDirection = AgentDirection.Right; break;
            }

            Debug.Log(returnDirection);
            return returnDirection;
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
