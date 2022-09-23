using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

#if UNITY_2021_2_0_OR_NEWER
using UnityEditor.SceneManagement;
#elif UNITY_2018_3_0_OR_NEWER || UNITY_2019_1_OR_NEWER

#endif

#endif




namespace MText
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class MText_TextUpdater : MonoBehaviour
    {

#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField] int openTime = 0;
#endif

        Modular3DText Text => gameObject.GetComponent<Modular3DText>();

        bool inPrefabStageOpened;


        [ExecuteAlways]
        private void Awake()
        {
#if UNITY_EDITOR
            openTime++;
            if (openTime < 2)
                return;
#endif

            if (!Text)
                return;

            if (EmptyText(Text))
                Text.UpdateText();

#if UNITY_EDITOR
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;

#if UNITY_2021_2_0_OR_NEWER || UNITY_2018_3_0_OR_NEWER || UNITY_2019_1_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageOpened;
#endif

#endif
        }



        private bool EmptyText(Modular3DText text)
        {
            if (string.IsNullOrEmpty(text.Text))
            {
                return false;
            }

            else if (text.characterObjectList.Count > 0)
            {
                for (int i = 0; i < text.characterObjectList.Count; i++)
                {
                    if (text.characterObjectList[i])
                    {
                        return false;
                    }
                }
            }

            if (gameObject.GetComponent<MeshFilter>())
            {
                if (gameObject.GetComponent<MeshFilter>().sharedMesh != null)
                {
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        [ExecuteInEditMode]
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Undo.undoRedoPerformed += UpdateText;
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                Undo.undoRedoPerformed -= UpdateText;
                //TODO on prefab stage opened/closed
            }
        }


#if UNITY_2021_2_0_OR_NEWER || UNITY_2018_3_0_OR_NEWER || UNITY_2019_1_OR_NEWER

        private void OnPrefabStageOpened(UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            if (!this)
            {
                UnityEditor.SceneManagement.PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
                return;
            }


            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosed;

            if (prefabStage.IsPartOfPrefabContents(gameObject))
            {
                inPrefabStageOpened = true;
            }

        }
        private void OnPrefabStageClosed(UnityEditor.SceneManagement.PrefabStage prefabStage)
        {

            if (!this)
            {
                UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosed;
                return;
            }

            inPrefabStageOpened = false;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosed;
        }

        void OnPrefabInstanceUpdated(GameObject instance)
        {
            if (!this)
                return;

            if (!inPrefabStageOpened)
                UpdateText();
        }
#endif //End of unity versions check




        private void UpdateText()
        {
            if (!this)
                return;

            if (gameObject.GetComponent<Modular3DText>())
            {
                gameObject.GetComponent<Modular3DText>().UpdateText();
            }
            else
            {
                //There shouldn't be almost any case where this called as long as RequiredComponentExist. "Almost"
                Debug.LogWarning("MText_TextUpdater component attached to gameobject without Modular3DText component.\nAdd the Modulur3DText component or remove this TextUpdater");
            }
        }
#endif //End of Unity Editor only check
    }
}