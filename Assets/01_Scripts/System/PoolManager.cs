using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    private Dictionary<Type, Queue<Component>> poolDictionary = new();

    private Dictionary<Type, Transform> poolParent = new Dictionary<Type, Transform>();

    private Dictionary<Type, Component> prefabDict = new Dictionary<Type, Component>();

    private Transform poolRoot;

    private void Start()
    {
        InitializeRoot();
    }

    private void InitializeRoot()
    {
        if (poolRoot != null) return;
        
        GameObject rootObj = new GameObject("PoolRoot");
        rootObj.transform.SetParent(transform);
        poolRoot = rootObj.transform;
    }

    public void PreLoadPool<T>(T prefab, int count) where T : Component
    {
        Type type = typeof(T);

        CreatePool(type);

        if (!prefabDict.ContainsKey(type))
        {
            prefabDict.Add(type, prefab);
        }

        for (int i = 0; i < count; i++)
        {
            // T 타입 오브젝트 생성
            T obj = Instantiate(prefab);
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(poolParent[type]);

            // 생성한 오브젝트를 해당하는 타입 큐에 저장
            poolDictionary[type].Enqueue(obj);
        }
    }

    // 풀 프리로드 시 저장된 프리팹 타입으로 풀에서 꺼내는 메서드
    public T GetPool<T>() where T : Component
    {
        Type type = typeof(T);

        if (!prefabDict.ContainsKey(type))
        {
            Debug.LogWarning($"{type.Name} 프리팹이 PoolManager에 등록되지 않음");
            return null;
        }

        T obj = null;

        if (poolDictionary[type].Count > 0)
        {
            obj = poolDictionary[type].Dequeue() as T;
        }
        else
        {
            obj = Instantiate(prefabDict[type] as T);
            obj.transform.SetParent(poolParent[type]);
        }

        obj.gameObject.SetActive(true);

        return obj;
    }

    public T GetPool<T>(T prefab) where T : Component
    {
        Type type = typeof(T);

        CreatePool(type);

        T obj = null;

        // 큐에 대기 중인 오브젝트가 있다면 해당 오브젝트 사용
        if (poolDictionary[type].Count > 0)
        {
            obj = poolDictionary[type].Dequeue() as T;
        }
        else
        {
            // 대기 중인 오브젝트가 없으면 새로 생성
            obj = Instantiate(prefab);
            obj.transform.SetParent(poolParent[type]);
        }
        // 사용할 오브젝트 활성화
        obj.gameObject.SetActive(true);

        return obj;
    }

    // 사용이 끝난 오브젝트를 풀에 반환
    public void ReturnPool<T>(T obj) where T : Component
    {
        Type type = typeof(T);

        CreatePool(type);

        obj.gameObject.SetActive(false);

        // 만약 다른 부모 밑에 있으면 타입별 풀 부모 밑으로 옮김
        if (obj.transform.parent != poolParent[type])
        {
            obj.transform.SetParent(poolParent[type]);
        }
        // 큐(풀)에 다시 삽입
        poolDictionary[type].Enqueue(obj);
    }

    // 해당 타입의 풀이 이미 있으면 즉시 리턴
    // 없다면 큐와 부모 오브제트를 만듦
    private void CreatePool(Type type)
    {
        if (poolDictionary.ContainsKey(type))
        {
            return;
        }
        // 해당 타입의 큐를 생성해 딕셔너리에 추가
        poolDictionary.Add(type, new Queue<Component>());
        // 부모 생성 호출
        CreatePoolParent(type);
    }

    // 타입별 부모 오브젝트를 생성
    private void CreatePoolParent(Type type)
    {
        // 타입 이름으로 생성
        GameObject parentObj = new GameObject(type.Name + "_Pool");

        parentObj.transform.SetParent(poolRoot);

        poolParent.Add(type, parentObj.transform);
    }
}
