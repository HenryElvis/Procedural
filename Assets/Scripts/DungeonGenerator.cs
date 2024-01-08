
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    }

    public int positionX;
    public int positionY;

    public int depth = 5;

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
        m_agent.Init();
        if (ActivateDebugTiles) DebugOffset = transform.position - new Vector3(m_agent.positionX, m_agent.positionY, 0);
        SpawnBackgroundDebugTiles(m_agent);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DestroyDebugTiles();
            m_agent.Init();
            path = GeneratePath(m_agent);
        }

    }

    public bool[,] GeneratePath(Agent agent)
    {
        bool[,] returnedPath = new bool[(agent.depth * 2)+1 , (agent.depth * 2) + 1];

        //Set Values to false
        for(int i  = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                returnedPath[i, j] = false;
            }
        }


        returnedPath[agent.positionX, agent.positionY] = true;

        if (ActivateDebugTiles)
            spawnDebugTile(agent.positionX, agent.positionY);


        //Walk
        for (int i = 0; i < agent.depth; i++)
        {
            agent.direction = GetRandomDirection(agent.direction);
            switch(agent.direction)
            {
                case AgentDirection.Left: agent.positionX -= 1; break;
                case AgentDirection.Right: agent.positionX += 1; break;
                case AgentDirection.Up: agent.positionY += 1; break;
                case AgentDirection.Down: agent.positionY -= 1; break;
            }
            
            if (returnedPath[agent.positionX, agent.positionY] == true)
                continue;

            
            returnedPath[agent.positionX, agent.positionY] = true;

            if (ActivateDebugTiles)
                spawnDebugTile(agent.positionX, agent.positionY);
        }

        return returnedPath;


        AgentDirection GetRandomDirection(AgentDirection currentDirection)
        {
            float keepdir = UnityEngine.Random.Range(0f, 1f);
            if (keepdir <= agent.KeepDirection && agent.direction != AgentDirection.None)
                return currentDirection;

            AgentDirection returnDirection = AgentDirection.None;
            AgentDirection backwardDirection = AgentDirection.None;

            switch(currentDirection){
                case AgentDirection.Up: backwardDirection = AgentDirection.Down; break;
                case AgentDirection.Down: backwardDirection = AgentDirection.Up; break;
                case AgentDirection.Left: backwardDirection = AgentDirection.Right; break;
                case AgentDirection.Right: backwardDirection = AgentDirection.Left; break;
            }

            int randomDirection = UnityEngine.Random.Range(1, 5);

            while(randomDirection == (int)backwardDirection)
            {
                randomDirection = UnityEngine.Random.Range(1, 5);
            }

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
            DebugTiles.Add(Instantiate(DebugTile_Fill, new Vector3(posX, posY, 0) + DebugOffset, Quaternion.identity));
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
