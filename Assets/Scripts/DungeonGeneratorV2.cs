using System;
using System.Collections.Generic;
using CreativeSpore.SuperTilemapEditor;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class DungeonGeneratorV2 : MonoBehaviour
{
    private class Agent
    {
        public Vector2Int currentPosition = Vector2Int.zero;
        public int currentDepth = 0;
        private Utils.ORIENTATION lastDirection = Utils.ORIENTATION.NONE;

        public void RandomlyAdvance(Utils.ORIENTATION growthDirection)
        {
            var nextMove = Random.Range(0, 3);
            switch (nextMove)
            {
                case 0:
                    if(lastDirection == Utils.ORIENTATION.NONE || lastDirection == growthDirection)
                        MoveDirection(Utils.AngleToOrientation(90, growthDirection));
                    else MoveDirection(lastDirection);
                    break;
                case 1:
                    if(lastDirection == Utils.ORIENTATION.NONE || lastDirection == growthDirection)
                        MoveDirection(Utils.AngleToOrientation(-90, growthDirection));
                    else MoveDirection(growthDirection);
                    break;
                default:
                    MoveDirection(growthDirection);
                    break;
            }
        }

        private void MoveDirection(Utils.ORIENTATION orientation)
        {
            switch (orientation)
            {
                case Utils.ORIENTATION.NORTH:
                    currentPosition.y += 1;
                    break;
                case Utils.ORIENTATION.EAST:
                    currentPosition.x += 1;
                    break;
                case Utils.ORIENTATION.SOUTH:
                    currentPosition.y -= 1;
                    break;
                case Utils.ORIENTATION.WEST:
                    currentPosition.x -= 1;
                    break;
            }

            lastDirection = orientation;
        }

        public void Init()
        {
            currentPosition = Vector2Int.zero;
            currentDepth = 0;
        }
    }

    private class GraphNode
    {
        public GraphNode previousNode;
        public Vector2Int position;
        public Room room;
        public bool isStairsRoom = false;

        public Vector3 ToWorldPosition()
        {
            return new Vector3(position.x * 11, position.y * 9);
        }
    }
    public static DungeonGeneratorV2 Instance = null;

    [Range(0, 300)] public int depth = 10;
    [Range(0, 200)] public int maxBranches = 3;
    [Range(0, 200)] public int maxBranchDepth = 5;
    public List<GameObject> roomPrefabs;
    public GameObject stairsRoom;
    public GameObject doorPrefab;

    private Agent _agent;
    private List<GraphNode> _graphNodes = new();
    private Room.Difficulty _currentDifficulty = Room.Difficulty.EASY;

    private GraphNode _lastNode = null;
    private bool _definedStairsRoom = false;

    private Vector3 _startPoint;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Player.Instance.gameObject.SetActive(false);

        _startPoint = Player.Instance.transform.position;
        _agent = new Agent();
        StartGeneration();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) IncrementDifficulty();
    }

    public void IncrementDifficulty()
    {
        switch (_currentDifficulty)
        {
            case Room.Difficulty.EASY: _currentDifficulty = Room.Difficulty.MEDIUM; break;
            case Room.Difficulty.MEDIUM: _currentDifficulty = Room.Difficulty.HARD; break;
        }

        StartGeneration();
    }

    private void StartGeneration()
    {
        Player.Instance.gameObject.SetActive(false);
        Player.Instance.gameObject.transform.position = _startPoint;

        ClearGraph();
        _definedStairsRoom = false;

        _agent.Init();
        GenerateGraph();
        SpawnGraph();

        Player.Instance.gameObject.SetActive(true);

        Debug.Log(_graphNodes.Count + " Rooms");
    }

    private void GenerateGraph()
    {
        void GenerateMainPath()
        {
            for (int i = 0; i < depth; i++)
            {
                var newNode = new GraphNode();
                newNode.position = _agent.currentPosition;
                newNode.previousNode = _lastNode;
                _graphNodes.Add(newNode);

                _agent.RandomlyAdvance(Utils.ORIENTATION.EAST);
                _agent.currentDepth++;
                _lastNode = newNode;
            }
        }

        void GenerateBranches()
        {
            if (maxBranches <= 0) return;

            var currentBranches = 0;

            for (int i = 0; i < depth; i++)
            {
                if(currentBranches >= maxBranches) break;

                _agent.currentPosition = _graphNodes[i].position;
                _agent.currentDepth = i;

                var doBranchHere = Random.Range(0, depth);
                if(doBranchHere < depth / 2) continue;

                _lastNode = _graphNodes[i];
                BranchHere(i);

                currentBranches++;
            }
        }

        void BranchHere(int nodeIndex)
        {
            var node = _graphNodes[nodeIndex];

            var emptyOrientation = FindEmptyAdjacent(node);
            if(emptyOrientation == Utils.ORIENTATION.NONE) return;

            _agent.currentPosition = node.position + Utils.OrientationToDir(emptyOrientation);

            var newNode = new GraphNode();
            newNode.position = _agent.currentPosition;
            newNode.previousNode = _lastNode;
            _graphNodes.Add(newNode);
            _lastNode = newNode;

            var depth = 0;
            while (true)
            {
                if(depth >= maxBranchDepth) break;

                _agent.RandomlyAdvance(emptyOrientation);
                var branchNode = FindNodeAtCoords(_agent.currentPosition);
                if(branchNode != null) break;

                var newBranchNode = new GraphNode();
                newBranchNode.position = _agent.currentPosition;
                newBranchNode.previousNode = _lastNode;
                _graphNodes.Add(newBranchNode);
                _lastNode = newBranchNode;

                depth++;
            }
        }

        void DefineStairsRoom()
        {
            _agent.Init();
            var random = Random.Range(0f, 1f);
            for (var i = 0; i < _graphNodes.Count; i++)
            {
                if (random > (float) i / _graphNodes.Count)
                    continue;

                _graphNodes[i].isStairsRoom = true;
                return;
            }
        }

        GenerateMainPath();
        GenerateBranches();

        DefineStairsRoom();
    }

    private void SpawnGraph()
    {
        for (var i = 0; i < _graphNodes.Count; i++)
        {
            SpawnRoom(_graphNodes[i], i == 0);
        }

        for (var i = 0; i < _graphNodes.Count; i++)
        {
            SpawnDoors(_graphNodes[i]);
        }
    }

    private void ClearGraph()
    {
        if(_graphNodes.Count == 0) return;

        foreach (var graphNode in _graphNodes)
        {
            Destroy(graphNode.room.gameObject);
        }

        _graphNodes.Clear();
    }

    private void SpawnRoom(GraphNode node, bool isStart = false)
    {
        var goFilteredByDifficulty = roomPrefabs.FindAll(x => x.GetComponent<Room>().diffuculty == _currentDifficulty);
        if (goFilteredByDifficulty.Count == 0)
        {
            Debug.Log("No room prefab found for difficulty " + _currentDifficulty);
            return;
        }

        var roomPrefab = node.isStairsRoom ? stairsRoom : goFilteredByDifficulty[Random.Range(0, goFilteredByDifficulty.Count)];

        var newGo = Instantiate(roomPrefab, node.ToWorldPosition(), Quaternion.identity);
        node.room = newGo.GetComponent<Room>();
        node.room.position = node.position;
        node.room.isStartRoom = isStart;
        node.room.isStairsRoom = node.isStairsRoom;

        node.room.SetLastRoom(node.previousNode?.room);
    }

    private void SpawnDoors(GraphNode node)
    {
        STETilemap tilemap = node.room.transform.GetChild(0).GetComponent<STETilemap>();

        var north = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.NORTH));
        if (north != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform).GetComponent<Door>();
            door.transform.localPosition = new Vector3(5.5f, 8.5f);
            door.SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var east = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.EAST));
        if (east != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(10.5f, 4.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var south = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.SOUTH));
        if (south != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(5.5f, 0.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var west = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.WEST));
        if (west != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(0.5f, 4.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        tilemap.UpdateMesh();

        node.room.RefreshDoors();
    }

    [CanBeNull]
    private GraphNode FindNodeAtCoords(Vector2Int vector)
    {
        var node = _graphNodes.Find(x => x.position.x == vector.x && x.position.y == vector.y);
        return node;
    }


    private Utils.ORIENTATION FindEmptyAdjacent(GraphNode node)
    {
        var north = _graphNodes.Find(x => x.position.x == node.position.x && x.position.y == node.position.y + 1);
        if (north == null) return Utils.ORIENTATION.NORTH;

        var east = _graphNodes.Find(x => x.position.x == node.position.x + 1 && x.position.y == node.position.y);
        if (east == null) return Utils.ORIENTATION.EAST;

        var south = _graphNodes.Find(x => x.position.x == node.position.x && x.position.y == node.position.y - 1);
        if (south == null) return Utils.ORIENTATION.SOUTH;

        var west = _graphNodes.Find(x => x.position.x == node.position.x - 1 && x.position.y == node.position.y);
        if (west == null) return Utils.ORIENTATION.WEST;

        return Utils.ORIENTATION.NONE;
    }
}
