using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject videur;
    [SerializeField] private string layerForProps = "props";

    public List<GameObject> props = new List<GameObject>();

    [SerializeField] private Camera _camera;

    private void Awake()
    {
        if (!_camera) _camera = Camera.main;
    }

    private void Start()
    {
        GameObject[] objects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.layer == LayerMask.NameToLayer(layerForProps))
                props.Add(obj);
        }

        EnableAllObjectWithSameLayer(false);
    }

    public void EnableAllObjectWithSameLayer(bool value)
    {
        foreach (GameObject obj in props)
            obj.SetActive(value);
    }

    public void InstantiateBoss()
    {
        Instantiate(videur, GetCameraCenter(), Quaternion.identity);
    }

    private Vector3 GetCameraCenter()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            Vector3 cameraCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10));

            return cameraCenter;
        }
        else
            return Vector3.zero;
    }
}
