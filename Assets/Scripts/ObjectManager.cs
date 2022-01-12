using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectManager : MonoBehaviour
{
    public GameObject objectToBeAdded;
    public GameObject addedObject;
    public GameObject targetObject;
    public GameObject pickedUpObject;
    public GameObject editingObject;

    public GameObject[] objectBlueprints;
    public GameObject[] objectPrefabs;

    public Material selectedMaterial;
    private Material oldMaterial;

    private int objectIndex;

    public FlexibleColorPicker fcp;
    public Vector3 sideOffset = new Vector3(150, 0);
    public Vector3 bottomOffset = new Vector3(0, 150);

    const string SAVE_PATH = "/save.txt";

    // Update is called once per frame
    void Update()
    {
        RevertMaterial();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool notOverUI = !EventSystem.current.IsPointerOverGameObject();

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {

            // If adding new object
            if (objectToBeAdded != null)
            {
                MoveToMouse(ray, hit, objectToBeAdded);
                MouseWheelRotate(objectToBeAdded);

                // Clicking to place the object
                if (Input.GetMouseButtonDown(0) && notOverUI)
                {
                    PlaceNewObject();
                }
                // Cancel the placement of an object
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Destroy(objectToBeAdded);
                }
            } else {
                SetTargetObject(hit);

                // If picking up object
                if (targetObject != null)
                {
                    if (Input.GetMouseButtonDown(0) && notOverUI && pickedUpObject == null)
                    {
                        PickUpObject(ray);
                    }
                    if (Input.GetKeyDown(KeyCode.Delete))
                    {
                        print("Deleting: " + targetObject.name);
                        DeleteObject();
                    }
                    if (Input.GetMouseButtonDown(1))
                    {
                        OpenColorMenu();
                    }
                }
                // Object is picked up
                else if (pickedUpObject != null)
                {
                    MoveToMouse(ray, hit, pickedUpObject);
                    MouseWheelRotate(pickedUpObject);
                    // Clicking to place the object
                    if (Input.GetMouseButtonDown(0) && notOverUI)
                    {
                        PlaceEditedObject();
                    }
                } else
                {
                    if (Input.GetMouseButtonDown(0) && notOverUI)
                    {
                        fcp.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void SetObjectColor()
    {
        if (editingObject != null)
        {
            editingObject.GetComponent<Renderer>().material.color = fcp.color;
        }
    }

    private void OpenColorMenu()
    {
        var colorMenuPosition = Input.mousePosition;
        editingObject = targetObject;
        fcp.gameObject.SetActive(true);

        var rightClamp = Screen.width - 200;
        var bottomClamp = 200;

        // Preventing the menu from leaving the screen
        if (colorMenuPosition.x > rightClamp)
            fcp.gameObject.transform.position = colorMenuPosition - sideOffset;
        else
            fcp.gameObject.transform.position = colorMenuPosition + sideOffset;

        if (colorMenuPosition.y < bottomClamp)
            fcp.gameObject.transform.position += bottomOffset;

        fcp.SetColor(oldMaterial.color);
    }

    private void DeleteObject()
    {
        Destroy(targetObject);
        if (fcp.gameObject.activeSelf)
        {
            fcp.gameObject.SetActive(false);
        }
    }

    private void SetTargetObject(RaycastHit hit)
    {
        if (hit.transform.gameObject.CompareTag("Selectable"))
        {
            targetObject = hit.transform.gameObject;
            ChangeMaterial(selectedMaterial);
        }
        else targetObject = null;
    }

    private void RevertMaterial()
    {
        if (targetObject != null && targetObject.CompareTag("Selectable"))
            targetObject.GetComponent<Renderer>().material = oldMaterial;
    }

    private void ChangeMaterial(Material mat)
    {
        oldMaterial = targetObject.GetComponent<Renderer>().material;
        targetObject.GetComponent<Renderer>().material = mat;
    }

    void MoveToMouse(Ray ray, RaycastHit hit, GameObject gameObject)
    {
        if (hit.normal == Vector3.up)
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            gameObject.transform.position = new Vector3(hit.point.x, hit.point.y + gameObject.GetComponent<Collider>().bounds.size.y / 2, hit.point.z);
        }   
    }

    public void SelectObject(int index)
    {
        GameObject newObject = objectBlueprints[index];
        objectIndex = index;
        if (objectToBeAdded != null)
        {
            Destroy(objectToBeAdded);
        }
        objectToBeAdded = Instantiate(newObject);    
        print("Selected: " + objectToBeAdded.name);
    }

    private void MouseWheelRotate(GameObject gameObject)
    {
        gameObject.transform.Rotate(Vector3.up * 10f * Input.mouseScrollDelta.y, Space.Self);
    }

    void PlaceNewObject()
    {
        addedObject = Instantiate(objectPrefabs[objectIndex], objectToBeAdded.transform.position, objectToBeAdded.transform.rotation);
        addedObject.transform.parent = transform;
        Destroy(objectToBeAdded);
        print("Placed a new Object " + addedObject.name);
    }

    private void PlaceEditedObject()
    {
        print("Placing: " + pickedUpObject);
        pickedUpObject.layer = LayerMask.NameToLayer("Default");
        pickedUpObject = null;
    }

    void PickUpObject(Ray ray)
    {
        pickedUpObject = targetObject;
        pickedUpObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        print("Picked up " + targetObject.name);
    }

    public void SaveObjects()
    {
        if (!File.Exists(Application.dataPath + SAVE_PATH))
            File.Create(Application.dataPath + SAVE_PATH);
        else
            File.WriteAllText(Application.dataPath + SAVE_PATH, "");

        print("Saving");
        File.AppendAllText(Application.dataPath + SAVE_PATH, "{ \"objects\" : [ ");

        for (int i = 0; i < transform.childCount; i++)
        {
            var childObject = transform.GetChild(i).gameObject;
            var color = childObject.GetComponent<MeshRenderer>().material.color;
            var childObjectname = childObject.name.Split('(')[0];
            string json = JsonUtility.ToJson(new SaveData(childObject.transform.position, childObject.transform.rotation, color, childObjectname));
            if (i != transform.childCount - 1)
            {
                json += ",";
            }
            File.AppendAllText(Application.dataPath + SAVE_PATH, json);
        }
        File.AppendAllText(Application.dataPath + SAVE_PATH, "]}");
    }

    public void LoadObjects()
    {
        print("Loading");
        if (File.Exists(Application.dataPath + SAVE_PATH))
        {
            string loadData = File.ReadAllText(Application.dataPath + SAVE_PATH);
            var data = JsonUtility.FromJson<SavedObjectList>(loadData);
            foreach (var item in data.objects)
            {
                GameObject current = null;
                // Instantiates the correct object prefab
                for (int i = 0; i < objectPrefabs.Length; i++)
                {
                    if (objectPrefabs[i].name == item.type)
                    {
                        current = Instantiate(objectPrefabs[i]);
                    }
                }

                // Assigns the saved values
                current.transform.parent = transform;
                current.transform.position = item.position;
                current.transform.rotation = item.rotation;
                current.GetComponent<MeshRenderer>().material.color = item.color;
            }
        } else
        {
            print("No save file.");
        }

    }
}

[Serializable]
class SaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public Color color;
    public string type;

    public SaveData(Vector3 position, Quaternion rotation, Color color, string type)
    {
        this.position = position;
        this.rotation = rotation;
        this.color = color;
        this.type = type;   
    }
}

[Serializable]
class SavedObjectList
{
    public List<SaveData> objects;
}
