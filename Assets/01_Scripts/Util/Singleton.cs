using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<T>();

            if (instance == null)
            {
                GameObject obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        T self = this as T;
        if (instance != null && instance != self)
        {
            Destroy(gameObject);
            return;
        }

        instance = self;
        if (transform.parent != null)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (ReferenceEquals(instance, this))
            instance = null;
    }
}
