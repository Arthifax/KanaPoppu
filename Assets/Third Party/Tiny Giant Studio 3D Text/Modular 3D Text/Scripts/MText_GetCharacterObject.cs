using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MText
{
    public class MText_GetCharacterObject
    {
        public static GameObject GetObject(char c, Modular3DText text)
        {
            GameObject obj = null;
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                if (text.pooling && text.pool)
                    obj = text.pool.GetPoolItem(text.Font, c);
                else
                {
                    Mesh meshPrefab = text.Font.RetrievePrefab(c);
                    if (meshPrefab)
                    {
                        obj = new GameObject { name = c.ToString() };
                        obj.AddComponent<MeshFilter>();
                        obj.GetComponent<MeshFilter>().sharedMesh = meshPrefab;

                    }
                }
            }
            else
            {
                if (text == null)
                    return null;

                Mesh meshPrefab = text.Font.RetrievePrefab(c);
                if (meshPrefab)
                {
                    obj = new GameObject { name = c.ToString() };
                    obj.SetActive(false);
                    obj.AddComponent<MeshFilter>();
                    obj.GetComponent<MeshFilter>().sharedMesh = meshPrefab;
                    obj.transform.SetParent(text.transform);
                }
            }
#else
            if(text.pooling && text.pool) 
                obj = text.pool.GetPoolItem(text.Font, c);                                      
            else
            {
                Mesh meshPrefab = text.Font.RetrievePrefab(c);
                if (meshPrefab)
                {
                    obj = new GameObject();
                    obj.AddComponent<MeshFilter>();
                    obj.GetComponent<MeshFilter>().sharedMesh = meshPrefab;
                    obj.name = c.ToString();
                }
            } 
#endif

            if (obj == null)
            {
#if UNITY_EDITOR
                //if (text.debugLogs && c != ' ')
                //    Debug.Log("<color=red>Missing mesh for character:</color>\n" + c);
#endif
                obj = new GameObject { name = "space" };
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (text.hideLettersInHierarchyInEditMode)
                //if (!text.ShouldItCreateChild() || text.hideLettersInHierarchyInEditMode)
                {
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    CheckLeftOver(obj, text);
                }
            }
            else
            {
                if (text.hideLettersInHierarchyInPlayMode)
                    obj.hideFlags = HideFlags.HideInHierarchy;
            }

            text._allcharacterObjectList.Add(obj);

            EditorUtility.SetDirty(obj);
#endif

            obj.SetActive(true);

            return obj;
        }

#if UNITY_EDITOR
        static void CheckLeftOver(GameObject obj, Modular3DText text)
        {
            if (text)
                return;

            obj.hideFlags = HideFlags.None;

            EditorApplication.delayCall += () =>
            {
                try { Object.DestroyImmediate(obj); }
                catch { }
            };
        }
#endif
    }
}