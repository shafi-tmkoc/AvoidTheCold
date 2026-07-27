using Sirenix.OdinInspector;
using UnityEngine;

namespace AvoidTheCold
{
public class Singleton<T> : SerializedMonoBehaviour where T : SerializedMonoBehaviour
{
    private static T _instance;

    public bool dontDestroyOnLoad = false;

    // Public static property to access the instance of the singleton
    public static T Instance => _instance;

    // Ensures the singleton is not destroyed when loading a new scene
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);  // Keeps the instance between scene transitions
        }
        else if (_instance != this)
        {
            Destroy(gameObject);  // Destroys duplicates
        }
    }
}
}