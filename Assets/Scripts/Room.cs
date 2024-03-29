﻿using CreativeSpore.SuperTilemapEditor;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Represents a single room in your dungeon
/// A room has an (i,j) integer position and a (di, dj) integer size in index coordinates
/// </summary>
public class Room : MonoBehaviour {

    public enum Difficulty
    {
        EASY,
        MEDIUM,
        HARD
    }

    public Difficulty diffuculty = Difficulty.EASY;
    public bool isStartRoom = false;
    public bool isStairsRoom = false;
    public bool hasSpawnedBoss = false;

    // Position of the room in index coordinates. Coordinates {0,0} are the coordinates of the central room. Room {1,0} is on the right side of room {0,0}.
	public Vector2Int position = Vector2Int.zero;
    // Size of the room in index coordinates. By default : {1,1}.
    public Vector2Int size = Vector2Int.one;


    private Room lastRoom;
    private Utils.ORIENTATION lastRoomOrientation;


    private TilemapGroup _tilemapGroup;
	private List<Door> doors = null;

    private bool _isInitialized = false;
    public static List<Room> allRooms { get; private set; } = new List<Room>();


    /// <summary>
    /// Get a list of all doors in a room. Do not use at Awake.
    /// </summary>
    public List<Door> GetDoors()
    {
        if (doors == null) {
            RefreshDoors();
        }
        return doors;
    }

    /// <summary>
    /// Returns the min and max tiles positions of room in local-space
    /// </summary>
    public void GetLocalTileBounds(out Vector2Int outMin, out Vector2Int outMax)
    {
        outMin = Vector2Int.zero;
        outMax = Vector2Int.zero;
        foreach (STETilemap tilemap in _tilemapGroup.Tilemaps)
        {
            outMin.x = Mathf.Min(tilemap.MinGridX, outMin.x);
            outMin.y = Mathf.Min(tilemap.MinGridY, outMin.y);
            outMax.x = Mathf.Max(tilemap.MaxGridX, outMax.x);
            outMax.y = Mathf.Max(tilemap.MaxGridY, outMax.y);
        }
    }

    /// <summary>
    /// Returns the bounding box of room in local-space
    /// </summary>
    public Bounds GetLocalBounds()
    {
        Initialize();
        Bounds roomBounds = new Bounds(Vector3.zero, Vector3.zero);
        if (_tilemapGroup == null)
            return roomBounds;

        foreach (STETilemap tilemap in _tilemapGroup.Tilemaps)
        {
            Bounds bounds = tilemap.MapBounds;
            roomBounds.Encapsulate(bounds);
        }
        return roomBounds;
    }

    /// <summary>
    /// Returns the bounding box of room in world-space
    /// </summary>
    public Bounds GetWorldBounds()
    {
        Bounds result = GetLocalBounds();
        result.center += transform.position;
        return result;
    }

    /// <summary>
    /// Check if a world-space point is included in room's bounding box
    /// </summary>
    public bool Contains(Vector3 point)
    {
        point.z = 0;
        return (GetWorldBounds().Contains(point));
    }

    /// <summary>
    /// On enter room is called when starting game on starting room or when walking through a door.
    /// May be overloaded for any purpose.
    /// </summary>
    /// <param name="from">The room player comes from. "null" when first called for the starting room.</param>
    public virtual void OnEnterRoom(Room from)
    {
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        Bounds cameraBounds = GetWorldBounds();
        cameraFollow.SetBounds(cameraBounds);

        if (isStairsRoom && !hasSpawnedBoss)
        {
            GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            gm.EnableAllObjectWithSameLayer(true);
            gm.InstantiateBoss();

            hasSpawnedBoss = true;
        }
    }

    /// <summary>
    /// Get adjacent room for given orientation (North, south, east or west)
    /// </summary>
    public Room GetAdjacentRoom(Utils.ORIENTATION orientation, Vector3 from)
    {
        Vector2Int dir = Utils.OrientationToDir(orientation);
        Vector2Int adjacentPos = position + dir + GetPositionOffset(from);
        Room adjacentRoom = Room.allRooms.Find(x =>
               adjacentPos.x >= x.position.x
            && adjacentPos.y >= x.position.y
            && adjacentPos.x < x.position.x + x.size.x
            && adjacentPos.y < x.position.y + x.size.y);
        return adjacentRoom;
    }

    /// <summary>
    /// Get door for given orientation (North, south, east or west) and a given world point (for room with size greater than one)
    /// </summary>
    public Door GetDoor(Utils.ORIENTATION orientation, Vector3 from)
    {
        return GetDoor(orientation, GetPositionOffset(from));        
    }

    /// <summary>
    /// Get door for given orientation (North, south, east or west) for room with size of one by one.
    /// </summary>
    public Door GetDoor(Utils.ORIENTATION orientation)
    {
        Debug.Assert(size != Vector2Int.one, "GetDoor(orientation) should only be used on room of size {1,1}. In other cases, please use: GetDoor(orientation,offset)");        
        return GetDoor(orientation, Vector2Int.zero);
    }

    /// <summary>
    /// Get door for given orientation (North, south, east or west) and a given local offset in index coordinate for room with size greater than one.
    /// </summary>
    public Door GetDoor(Utils.ORIENTATION orientation, Vector2Int offset)
    {
        Vector2Int doorPosition = position + offset;
        List<Door> doors = GetDoors();
        foreach (Door door in doors)
        {
            if (doorPosition == doorPosition + GetPositionOffset(door.transform.position)
                && door.Orientation == orientation)
            {
                return door;
            }
        }
        return null;
    }
    #region Internal 

    void Awake()
    {
		allRooms.Add(this);
		Initialize();
	}

	void Start()
	{
		// RefreshDoors();

        if (isStartRoom)
        {
            Player.Instance.EnterRoom(this);
        }
    }

    public void SetLastRoom(Room room)
    {
        lastRoom = room;
        DefineLastRoomOrientation();
    }

	public void RefreshDoors()
	{
		if(doors == null) {
            doors = new List<Door>();
        } else {
			doors.Clear();
        }
        GetComponentsInChildren<Door>(true, doors);

        var keys = GetComponentsInChildren<KeyCollectible>(true).Length;
        if(keys == 0) return;

        for (int i = 0; i < keys; i++)
        {
            var openDoors = doors.FindAll(x => x.State == Door.STATE.OPEN && lastRoomOrientation != x.Orientation);
            if(openDoors.Count == 0) break;

            var random = openDoors[Random.Range(0, openDoors.Count)];
            random.SetState(Door.STATE.CLOSED);
        }
    }

    /// <summary>
    /// Get position offset in index for a point in world coordinates inside rooms with a index size greater than {1,1}
    /// </summary>
    public Vector2Int GetPositionOffset(Vector3 worldPoint)
    {
        if (size.x <= 1 && size.y <= 1)
            return Vector2Int.zero;

        Vector2Int offset = Vector2Int.zero;
        Bounds bounds = GetWorldBounds();
        Vector3 localPoint = worldPoint - bounds.min;
        if(size.x > 1)
        {
            offset.x = Mathf.Clamp((int)(localPoint.x / (bounds.size.x / size.x)), 0, size.x-1);
        }
        if (size.y > 1)
        {
            offset.y = Mathf.Clamp((int)(localPoint.y / (bounds.size.y / size.y)), 0, size.y-1);
        }
        return offset;
    }

    private void Initialize()
    {
		if (_isInitialized)
			return;
		_tilemapGroup = GetComponentInChildren<TilemapGroup>();
		foreach (STETilemap tilemap in _tilemapGroup.Tilemaps)
		{
			tilemap.RecalculateMapBounds();
		}
		_isInitialized = true;
	}

    private void DefineLastRoomOrientation()
    {
        if (lastRoom == null)
        {
            lastRoomOrientation = Utils.ORIENTATION.NONE;
            return;
        }

        var north = GetAdjacentRoom(Utils.ORIENTATION.NORTH, transform.position);
        if (north == lastRoom)
        {
            lastRoomOrientation = Utils.ORIENTATION.NORTH;
            return;
        }

        var east = GetAdjacentRoom(Utils.ORIENTATION.EAST, transform.position);
        if (east == lastRoom)
        {
            lastRoomOrientation = Utils.ORIENTATION.EAST;
            return;
        }

        var south = GetAdjacentRoom(Utils.ORIENTATION.SOUTH, transform.position);
        if (south == lastRoom)
        {
            lastRoomOrientation = Utils.ORIENTATION.SOUTH;
            return;
        }

        var west = GetAdjacentRoom(Utils.ORIENTATION.WEST, transform.position);
        if (west == lastRoom)
        {
            lastRoomOrientation = Utils.ORIENTATION.WEST;
            return;
        }
    }

	private void OnDestroy()
	{
		allRooms.Remove(this);
	}
    #endregion Internal 
}
